using Entities.Types.Enums;
using Entry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

ServiceProvider serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole()
        )
    .BuildServiceProvider();

var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

var dataGetter = new DataGetterEntry(loggerFactory);
var runModeEnv = Environment.GetEnvironmentVariable("RUN_MODE");
var settings = new ModeSettings()
{
    ConnectionString = Environment.GetEnvironmentVariable("NHL_DATABASE") ?? string.Empty,
    Mode = ModeTypeParser.ParseFromString(runModeEnv),
    OddsApiKey = Environment.GetEnvironmentVariable("ODDS_API_KEY") ?? string.Empty,
    OddsApiBackfillKey = Environment.GetEnvironmentVariable("API_BACKFILL_KEY") ?? string.Empty,
    ThrottleTimeMs = int.Parse(Environment.GetEnvironmentVariable("THROTTLE_TIME_MS") ?? "0"),
    BackfillGameIds = (Environment.GetEnvironmentVariable("BACKFILL_GAME_IDS") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(int.Parse)
        .ToList(),
    RefetchDays = int.Parse(Environment.GetEnvironmentVariable("REFETCH_DAYS") ?? "7"),
    CleanParallelism = int.Parse(Environment.GetEnvironmentVariable("CLEAN_PARALLELISM") ?? "4"),
    BackfillStartYear = int.TryParse(Environment.GetEnvironmentVariable("BACKFILL_START_YEAR"), out var backfillStart) ? backfillStart : null,
};

if (string.IsNullOrEmpty(settings.ConnectionString))
{
    var config = new ConfigurationBuilder().AddJsonFile("appsettings.Local.json").Build();
    if (runModeEnv == null)
        settings.Mode = ModeTypeParser.ParseFromString(config["ModeSettings:RUN_MODE"]);
    // Like RUN_MODE, the file only fills in what the environment didn't set
    if (Environment.GetEnvironmentVariable("THROTTLE_TIME_MS") == null)
        settings.ThrottleTimeMs = int.Parse(config["ModeSettings:THROTTLE_TIME_MS"] ?? "0");
    settings.ConnectionString = config.GetConnectionString("NHL_DATABASE") ?? string.Empty;
    if (string.IsNullOrEmpty(settings.OddsApiKey))
        settings.OddsApiKey = config["OddsApi:API_FUTURE_KEY"] ?? string.Empty;
    if (string.IsNullOrEmpty(settings.OddsApiBackfillKey))
        settings.OddsApiBackfillKey = config["OddsApi:API_BACKFILL_KEY"] ?? string.Empty;
    var gameIdsConfig = config["ModeSettings:BACKFILL_GAME_IDS"];
    if (!settings.BackfillGameIds.Any() && !string.IsNullOrEmpty(gameIdsConfig))
        settings.BackfillGameIds = gameIdsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse);
}

if (string.IsNullOrEmpty(settings.ConnectionString))
    throw new Exception("Connection String Null");

var jobName = settings.Mode switch
{
    ModeType.NhlAdd or ModeType.NhlUpdate => "data-collection",
    ModeType.NextDayOdds => "odds-fetch",
    ModeType.BackfillOdds => "odds-backfill",
    ModeType.KalshiFetch => "kalshi-fetch",
    ModeType.BackfillKalshi => "kalshi-backfill",
    ModeType.BackfillGame => "game-backfill",
    ModeType.BackfillGoalieStats => "goalie-stats-backfill",
    ModeType.CleanAll => "clean-all",
    ModeType.LineupSnapshot => "lineup-snapshot",
    ModeType.BackfillGameDetails => "game-details-backfill",
    _ => null,
};

await UpdateJobStatus(settings.ConnectionString, jobName, "running", null);
try
{
    await dataGetter.Main(settings);
    await UpdateJobStatus(settings.ConnectionString, jobName, "completed", null);
}
catch (Exception ex)
{
    await UpdateJobStatus(settings.ConnectionString, jobName, "failed", ex.Message);
    throw;
}

static async Task UpdateJobStatus(string connectionString, string? jobName, string status, string? error)
{
    if (jobName == null) return;
    try
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO "JobStatus" ("JobName", "Status", "StartedAt", "FinishedAt", "Error")
            VALUES (@name, @status,
                    CASE WHEN @status = 'running' THEN @now ELSE NULL END,
                    CASE WHEN @status != 'running' THEN @now ELSE NULL END,
                    @error)
            ON CONFLICT ("JobName") DO UPDATE SET
                "Status" = @status,
                "StartedAt" = CASE WHEN @status = 'running' THEN @now ELSE "JobStatus"."StartedAt" END,
                "FinishedAt" = CASE WHEN @status != 'running' THEN @now ELSE NULL END,
                "Error" = @error
            """, conn);
        cmd.Parameters.AddWithValue("name", jobName);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("error", (object?)error ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }
    catch
    {
        // Don't fail the main job if status tracking fails
    }
}