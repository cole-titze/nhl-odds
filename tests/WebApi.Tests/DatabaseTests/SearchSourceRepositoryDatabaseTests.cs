using DatabaseAccess;
using DatabaseAccess.SearchSourceRepository;
using Entities.DbModels;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Tests.DatabaseTests;

/// <summary>
/// The search index's player rows against a real Postgres loaded with database/Scripts/CreateTables.sql. Skipped
/// unless NHL_TEST_DATABASE holds a connection string to a throwaway database: each test empties the tables it uses.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SearchSourceRepositoryDatabaseTests
{
    private const int TOR = 10;
    private const int BOS = 6;
    private static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    private const int TRADED = 1;     // last game for TOR; the roster report now lists him for BOS
    private const int SENT_DOWN = 2;  // last game for TOR this season; on no roster now
    private const int RETIRED = 3;    // last game for TOR in 2012

    private NhlDbContext _db = null!;

    [TestInitialize]
    public async Task Seed()
    {
        var connectionString = Environment.GetEnvironmentVariable("NHL_TEST_DATABASE");
        if (string.IsNullOrEmpty(connectionString))
            Assert.Inconclusive("NHL_TEST_DATABASE isn't set");
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        _db = new NhlDbContext(connectionString);
        await _db.Database.ExecuteSqlRawAsync("""
            TRUNCATE "RosterStatus", "GameRosterSpot", "GameSkaterStats", "GameGoalieStats", "GameRaw", "Player", "Team" CASCADE
            """);
        await _db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO "Team" ("Id", "Abbreviation", "FranchiseId", "LeagueId") VALUES ({TOR}, 'TOR', 5, 133), ({BOS}, 'BOS', 6, 133)
            """);
        foreach (var id in new[] { TRADED, SENT_DOWN, RETIRED })
            // A stale CurrentTeamId (BOS for everyone) that must not be used
            _db.Player.Add(new DbPlayer { Id = id, FirstName = "P", LastName = $"Player{id}", CurrentTeamId = BOS, BirthDate = new DateTime(1995, 1, 1) });
        foreach (var (id, season) in new[] { (2026020001, 2026), (2011020500, 2011) })
            _db.GameRaw.Add(new DbGameRaw
            {
                Id = id, SeasonStartYear = season, GameDateUTC = new DateTime(season, 10, 10, 23, 0, 0), HomeTeamId = TOR,
                AwayTeamId = BOS, HasBeenPlayed = true, GameType = Entities.Types.Enums.GameType.Regular,
            });
        _db.GameSkaterStats.Add(new DbGameSkaterStats { GameId = 2026020001, PlayerId = TRADED, TeamId = TOR, TimeOnIceSeconds = 900 });
        _db.GameSkaterStats.Add(new DbGameSkaterStats { GameId = 2026020001, PlayerId = SENT_DOWN, TeamId = TOR, TimeOnIceSeconds = 900 });
        _db.GameSkaterStats.Add(new DbGameSkaterStats { GameId = 2011020500, PlayerId = RETIRED, TeamId = TOR, TimeOnIceSeconds = 900 });
        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public async Task Close()
    {
        if (_db != null)
            await _db.DisposeAsync();
    }

    private async Task<Dictionary<int, int?>> CurrentTeams() =>
        (await new SearchSourceRepository(_db).GetPlayers(Now)).ToDictionary(p => p.Player.Id, p => p.CurrentTeamId);

    [TestMethod]
    public async Task GetPlayers_RecentRosterReport_IsTheCurrentTeam()
    {
        _db.RosterStatus.Add(new DbRosterStatus { SnapshotUTC = new DateTime(2026, 11, 7, 6, 0, 0), PlayerId = TRADED, TeamId = BOS });
        await _db.SaveChangesAsync();

        var teams = await CurrentTeams();

        teams[TRADED].Should().Be(BOS);
        teams[SENT_DOWN].Should().BeNull();
        teams[RETIRED].Should().BeNull();
    }

    [TestMethod]
    public async Task GetPlayers_NoRecentReport_ThisSeasonsLatestGame()
    {
        _db.RosterStatus.Add(new DbRosterStatus { SnapshotUTC = new DateTime(2026, 10, 20), PlayerId = TRADED, TeamId = BOS });
        await _db.SaveChangesAsync();

        var teams = await CurrentTeams();

        teams[TRADED].Should().Be(TOR);
        teams[SENT_DOWN].Should().Be(TOR);
        teams[RETIRED].Should().BeNull();
    }
}
