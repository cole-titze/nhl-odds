using Npgsql;
using Testcontainers.PostgreSql;

namespace Integration.Tests;

/// <summary>
/// One Postgres database for the whole test run, built from database/Scripts/CreateTables.sql
/// (the schema's source of truth, so no EF migrations or EnsureCreated).
///
/// By default it starts postgres:18-alpine in Docker with Testcontainers. Setting
/// INTEGRATION_DATABASE to a connection string uses that server instead: a fresh database is
/// created on it for the run and dropped afterwards. Without either, the tests are skipped
/// (Inconclusive) locally and fail in CI (CI=true), so a CI run can't pass without running them.
/// </summary>
[TestClass]
public static class TestDatabase
{
    public const string Category = "Integration";

    private const string Image = "postgres:18-alpine";

    private static PostgreSqlContainer? _container;
    private static string? _serverConnectionString;
    private static string? _databaseName;
    private static string? _unavailable;

    public static string ConnectionString { get; private set; } = "";

    [AssemblyInitialize]
    public static async Task Start(TestContext _)
    {
        // Same switch the WebApi and Entry set at startup: timestamp columns read back as
        // DateTimeKind.Unspecified, as they do in production
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        try
        {
            var server = Environment.GetEnvironmentVariable("INTEGRATION_DATABASE");
            if (!string.IsNullOrEmpty(server))
            {
                _serverConnectionString = server;
                _databaseName = $"nhl_it_{Guid.NewGuid():N}";
                await Execute(server, $"CREATE DATABASE \"{_databaseName}\"");
                ConnectionString = new NpgsqlConnectionStringBuilder(server) { Database = _databaseName }.ConnectionString;
            }
            else
            {
                _container = new PostgreSqlBuilder(Image)
                    .WithDatabase("nhl")
                    .Build();
                await _container.StartAsync();
                ConnectionString = _container.GetConnectionString();
            }
        }
        catch (Exception ex)
        {
            _unavailable = $"No test database: set INTEGRATION_DATABASE or start Docker ({ex.GetType().Name}: {ex.Message})";
            return;
        }

        var schema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "CreateTables.sql"));
        await Execute(ConnectionString, schema);
    }

    [AssemblyCleanup]
    public static async Task Stop()
    {
        NpgsqlConnection.ClearAllPools();
        if (_container is not null)
            await _container.DisposeAsync();
        if (_serverConnectionString is not null && _databaseName is not null)
            await Execute(_serverConnectionString, $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
    }

    /// <summary>Empties every table. Call at the start of each test.</summary>
    public static async Task Reset()
    {
        if (_unavailable is not null)
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
                Assert.Fail(_unavailable);
            Assert.Inconclusive(_unavailable);
        }

        await Execute(ConnectionString, """
            DO $$
            DECLARE tables TEXT;
            BEGIN
                SELECT string_agg(format('%I.%I', schemaname, tablename), ', ') INTO tables
                FROM pg_tables WHERE schemaname = 'public';
                IF tables IS NOT NULL THEN
                    EXECUTE 'TRUNCATE ' || tables || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
            """);
    }

    public static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
