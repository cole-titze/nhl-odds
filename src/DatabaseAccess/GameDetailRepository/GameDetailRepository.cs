using System.Data;
using Entities.DbModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace DatabaseAccess.GameDetailRepository;

/// <summary>
/// Saves game rosters, shift charts and goal replays. Rows go in with binary COPY: the backfill writes about 17M shift
/// rows and 47M replay positions, which EF inserts row by row far too slowly.
/// </summary>
public class GameDetailRepository : IGameDetailRepository
{
    private readonly NhlDbContext _dbContext;

    public GameDetailRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<int>> GetPlayedGamesWithoutFetch(int seasonStartYear, string kind, bool includeEmpty = false)
    {
        return _dbContext.GameRaw.AsNoTracking()
            .Where(g => g.SeasonStartYear == seasonStartYear && g.HasBeenPlayed
                && !_dbContext.GameDetailFetch.Any(f => f.GameId == g.Id && f.Kind == kind && (!includeEmpty || f.Rows > 0)))
            .OrderBy(g => g.Id)
            .Select(g => g.Id)
            .ToListAsync();
    }

    public async Task<List<(int GameId, int EventId, string Url)>> GetGoalsWithoutReplay(int seasonStartYear)
    {
        var goals = await _dbContext.GameGoalEvent.AsNoTracking()
            .Where(g => g.Game!.SeasonStartYear == seasonStartYear && g.PptReplayUrl != ""
                && !_dbContext.GoalReplay.Any(r => r.GameId == g.GameId && r.EventId == g.Id))
            .OrderBy(g => g.GameId).ThenBy(g => g.Id)
            .Select(g => new { g.GameId, g.Id, g.PptReplayUrl })
            .ToListAsync();
        return goals.Select(g => (g.GameId, g.Id, g.PptReplayUrl)).ToList();
    }

    public Task ReplaceRoster(int gameId, IReadOnlyCollection<DbGameRosterSpot> spots, DateTime fetchedUtc)
    {
        return InTransaction(async conn =>
        {
            await Execute(conn, """DELETE FROM "GameRosterSpot" WHERE "GameId" = @g""", ("g", gameId));
            await using (var copy = await conn.BeginBinaryImportAsync(
                """COPY "GameRosterSpot" ("GameId", "PlayerId", "TeamId", "SweaterNumber", "PositionCode") FROM STDIN (FORMAT BINARY)"""))
            {
                foreach (var s in spots)
                {
                    await copy.StartRowAsync();
                    await copy.WriteAsync(s.GameId, NpgsqlDbType.Integer);
                    await copy.WriteAsync(s.PlayerId, NpgsqlDbType.Integer);
                    await copy.WriteAsync(s.TeamId, NpgsqlDbType.Integer);
                    await WriteNullable(copy, s.SweaterNumber, NpgsqlDbType.Smallint);
                    await copy.WriteAsync(s.PositionCode, NpgsqlDbType.Varchar);
                }
                await copy.CompleteAsync();
            }
            await RecordFetch(conn, gameId, "Roster", fetchedUtc, spots.Count);
        });
    }

    public Task ReplaceShifts(int gameId, IReadOnlyCollection<DbGameShift> shifts, DateTime fetchedUtc)
    {
        return InTransaction(async conn =>
        {
            await Execute(conn, """DELETE FROM "GameShift" WHERE "GameId" = @g""", ("g", gameId));
            await using (var copy = await conn.BeginBinaryImportAsync("""
                COPY "GameShift" ("Id", "GameId", "PlayerId", "TeamId", "Period", "ShiftNumber", "StartSeconds", "EndSeconds",
                    "DurationSeconds", "TypeCode", "DetailCode", "EventNumber", "EventDescription", "EventDetails") FROM STDIN (FORMAT BINARY)
                """))
            {
                foreach (var s in shifts)
                {
                    await copy.StartRowAsync();
                    await copy.WriteAsync(s.Id, NpgsqlDbType.Bigint);
                    await copy.WriteAsync(s.GameId, NpgsqlDbType.Integer);
                    await copy.WriteAsync(s.PlayerId, NpgsqlDbType.Integer);
                    await copy.WriteAsync(s.TeamId, NpgsqlDbType.Integer);
                    await copy.WriteAsync(s.Period, NpgsqlDbType.Smallint);
                    await copy.WriteAsync(s.ShiftNumber, NpgsqlDbType.Smallint);
                    await copy.WriteAsync(s.StartSeconds, NpgsqlDbType.Smallint);
                    await copy.WriteAsync(s.EndSeconds, NpgsqlDbType.Smallint);
                    await WriteNullable(copy, s.DurationSeconds, NpgsqlDbType.Smallint);
                    await copy.WriteAsync(s.TypeCode, NpgsqlDbType.Smallint);
                    await copy.WriteAsync(s.DetailCode, NpgsqlDbType.Smallint);
                    await WriteNullable(copy, s.EventNumber, NpgsqlDbType.Integer);
                    await WriteNullable(copy, s.EventDescription, NpgsqlDbType.Text);
                    await WriteNullable(copy, s.EventDetails, NpgsqlDbType.Text);
                }
                await copy.CompleteAsync();
            }
            await RecordFetch(conn, gameId, "Shifts", fetchedUtc, shifts.Count);
        });
    }

    public Task ReplaceReplay(DbGoalReplay replay, IReadOnlyCollection<DbGoalReplayPosition> positions)
    {
        return InTransaction(async conn =>
        {
            await Execute(conn, """DELETE FROM "GoalReplayPosition" WHERE "GameId" = @g AND "EventId" = @e""",
                ("g", replay.GameId), ("e", replay.EventId));
            await Execute(conn, """DELETE FROM "GoalReplay" WHERE "GameId" = @g AND "EventId" = @e""",
                ("g", replay.GameId), ("e", replay.EventId));
            await Execute(conn, """
                INSERT INTO "GoalReplay" ("GameId", "EventId", "HttpStatus", "FrameCount", "FirstTimeStamp", "FetchedUTC")
                VALUES (@g, @e, @s, @f, @t, @at)
                """, ("g", replay.GameId), ("e", replay.EventId), ("s", replay.HttpStatus), ("f", replay.FrameCount),
                ("t", (object?)replay.FirstTimeStamp ?? DBNull.Value), ("at", replay.FetchedUTC));
            await using var copy = await conn.BeginBinaryImportAsync("""
                COPY "GoalReplayPosition" ("GameId", "EventId", "Frame", "TrackId", "TimeStamp", "PlayerId", "TeamId",
                    "SweaterNumber", "X", "Y") FROM STDIN (FORMAT BINARY)
                """);
            foreach (var p in positions)
            {
                await copy.StartRowAsync();
                await copy.WriteAsync(p.GameId, NpgsqlDbType.Integer);
                await copy.WriteAsync(p.EventId, NpgsqlDbType.Integer);
                await copy.WriteAsync(p.Frame, NpgsqlDbType.Smallint);
                await copy.WriteAsync(p.TrackId, NpgsqlDbType.Integer);
                await copy.WriteAsync(p.TimeStamp, NpgsqlDbType.Bigint);
                await WriteNullable(copy, p.PlayerId, NpgsqlDbType.Integer);
                await WriteNullable(copy, p.TeamId, NpgsqlDbType.Integer);
                await WriteNullable(copy, p.SweaterNumber, NpgsqlDbType.Smallint);
                await copy.WriteAsync(p.X, NpgsqlDbType.Real);
                await copy.WriteAsync(p.Y, NpgsqlDbType.Real);
            }
            await copy.CompleteAsync();
        });
    }

    private static Task RecordFetch(NpgsqlConnection conn, int gameId, string kind, DateTime fetchedUtc, int rows)
    {
        return Execute(conn, """
            INSERT INTO "GameDetailFetch" ("GameId", "Kind", "FetchedUTC", "Rows") VALUES (@g, @k, @at, @r)
            ON CONFLICT ("GameId", "Kind") DO UPDATE SET "FetchedUTC" = @at, "Rows" = @r
            """, ("g", gameId), ("k", kind), ("at", fetchedUtc), ("r", rows));
    }

    private async Task InTransaction(Func<NpgsqlConnection, Task> work)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        var opened = conn.State != ConnectionState.Open;
        if (opened)
            await conn.OpenAsync();
        try
        {
            await using var tx = await conn.BeginTransactionAsync();
            await work(conn);
            await tx.CommitAsync();
        }
        finally
        {
            if (opened)
                await conn.CloseAsync();
        }
    }

    private static async Task Execute(NpgsqlConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static Task WriteNullable<T>(NpgsqlBinaryImporter copy, T? value, NpgsqlDbType type)
    {
        return value is null ? copy.WriteNullAsync() : copy.WriteAsync(value, type);
    }
}