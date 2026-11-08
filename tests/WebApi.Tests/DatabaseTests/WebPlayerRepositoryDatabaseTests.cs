using DatabaseAccess;
using DatabaseAccess.WebPlayerRepository;
using Entities.DbModels;
using Entities.Types;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Tests.DatabaseTests;

/// <summary>
/// WebPlayerRepository against a real Postgres loaded with database/Scripts/CreateTables.sql. Skipped unless
/// NHL_TEST_DATABASE holds a connection string to a throwaway database: each test empties the tables it uses.
///
///   psql -d nhl_test -f database/Scripts/CreateTables.sql
///   NHL_TEST_DATABASE="Host=localhost;Database=nhl_test;Username=postgres;Password=..." dotnet test --filter WebPlayerRepositoryDatabaseTests
/// </summary>
[TestClass]
[DoNotParallelize]
public class WebPlayerRepositoryDatabaseTests
{
    private const int TOR = 10;
    private const int BOS = 6;
    private static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    // Player ids by story
    private const int TRADED = 1;       // played for TOR this season; the roster report now lists him for BOS
    private const int INJURED = 2;      // TOR, on injured reserve
    private const int SENT_DOWN = 3;    // played for TOR this season; on no roster now
    private const int RETIRED = 4;      // last game for TOR in April 2012
    private const int GOALIE = 5;       // played Oct 10, dressed as the backup Oct 20 without going in
    private const int CALLED_UP = 6;    // on TOR's report, no game for them this season
    private const int NO_GAMES = 7;     // a Player row and nothing else

    private const int GAME_OCT_10 = 2026020001;
    private const int GAME_OCT_20 = 2026020002;
    private const int GAME_LAST_SEASON = 2025020001;
    private const int GAME_2012 = 2011020500;

    private static string? ConnectionString => Environment.GetEnvironmentVariable("NHL_TEST_DATABASE");

    private GameDbContext _db = null!;

    [TestInitialize]
    public async Task Seed()
    {
        if (string.IsNullOrEmpty(ConnectionString))
            Assert.Inconclusive("NHL_TEST_DATABASE isn't set");
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        _db = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseNpgsql(ConnectionString).Options);
        await _db.Database.ExecuteSqlRawAsync("""
            TRUNCATE "RosterStatus", "GameRosterSpot", "GameSkaterStats", "GameGoalieStats", "GameRaw", "SeasonTeam", "Player", "Team" CASCADE
            """);

        // GameDbContext doesn't map the Team table, so it's seeded by hand
        await _db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO "Team" ("Id", "Abbreviation", "FranchiseId", "LeagueId") VALUES ({TOR}, 'TOR', 5, 133), ({BOS}, 'BOS', 6, 133)
            """);
        foreach (var season in new[] { 2011, 2025, 2026 })
        {
            _db.SeasonTeam.Add(new DbSeasonTeam { TeamId = TOR, SeasonStartYear = season, Abbreviation = "TOR", Name = "Toronto Maple Leafs", CommonName = "Maple Leafs", PlaceName = "Toronto" });
            _db.SeasonTeam.Add(new DbSeasonTeam { TeamId = BOS, SeasonStartYear = season, Abbreviation = "BOS", Name = "Boston Bruins", CommonName = "Bruins", PlaceName = "Boston" });
        }
        foreach (var id in new[] { TRADED, INJURED, SENT_DOWN, RETIRED, GOALIE, CALLED_UP, NO_GAMES })
            _db.Player.Add(new DbPlayer { Id = id, FirstName = "P", LastName = $"Player{id}", CurrentTeamId = TOR, BirthDate = new DateTime(1995, 1, 1) });

        Game(GAME_OCT_10, 2026, new DateTime(2026, 10, 10, 23, 0, 0));
        Game(GAME_OCT_20, 2026, new DateTime(2026, 10, 20, 23, 0, 0));
        Game(GAME_LAST_SEASON, 2025, new DateTime(2025, 10, 10, 23, 0, 0));
        Game(GAME_2012, 2011, new DateTime(2012, 4, 7, 23, 0, 0));

        Skater(GAME_OCT_10, TRADED, TOR);
        Skater(GAME_OCT_20, TRADED, TOR);
        Skater(GAME_OCT_10, INJURED, TOR);
        Skater(GAME_OCT_20, SENT_DOWN, TOR);
        Skater(GAME_2012, RETIRED, TOR);
        Skater(GAME_LAST_SEASON, CALLED_UP, TOR);
        _db.GameGoalieStats.Add(new DbGameGoalieStats { GameId = GAME_OCT_10, PlayerId = GOALIE, TeamId = TOR, TimeOnIceSeconds = 3600, IsStarter = true });
        _db.GameGoalieStats.Add(new DbGameGoalieStats { GameId = GAME_OCT_20, PlayerId = GOALIE, TeamId = TOR, TimeOnIceSeconds = 0 });
        _db.GameRosterSpot.Add(new DbGameRosterSpot { GameId = GAME_OCT_20, PlayerId = TRADED, TeamId = TOR, SweaterNumber = 34, PositionCode = "C" });
        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public async Task Close()
    {
        if (_db != null)
            await _db.DisposeAsync();
    }

    private void Game(int id, int season, DateTime startUtc, bool played = true) => _db.GameRaw.Add(new DbGameRaw
    {
        Id = id,
        SeasonStartYear = season,
        GameDateUTC = startUtc,
        HomeTeamId = TOR,
        AwayTeamId = BOS,
        HasBeenPlayed = played,
        GameType = (Entities.Types.Enums.GameType)2,
    });

    private void Skater(int gameId, int playerId, int teamId) => _db.GameSkaterStats.Add(new DbGameSkaterStats
    {
        GameId = gameId,
        PlayerId = playerId,
        TeamId = teamId,
        TimeOnIceSeconds = 900,
        Position = POSITION.Center,
    });

    private async Task Report(DateTime snapshotUtc, params (int PlayerId, int TeamId, bool InjuredReserve)[] rows)
    {
        _db.RosterStatus.AddRange(rows.Select(r => new DbRosterStatus
        {
            SnapshotUTC = DateTime.SpecifyKind(snapshotUtc, DateTimeKind.Unspecified),
            PlayerId = r.PlayerId,
            TeamId = r.TeamId,
            IsInjuredReserve = r.InjuredReserve,
        }));
        await _db.SaveChangesAsync();
    }

    private Task FreshReport() => Report(Now.AddHours(-6),
        (TRADED, BOS, false), (INJURED, TOR, true), (GOALIE, TOR, false), (CALLED_UP, TOR, false));

    private WebPlayerRepository Repository() => new(_db, new FixedClock(Now));

    private sealed class FixedClock(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utc);
    }

    [TestMethod]
    public async Task GetPlayer_TradedSinceLastGame_CurrentTeamFromRosterReport()
    {
        await FreshReport();

        var player = await Repository().GetPlayer(TRADED);

        player!.CurrentTeam!.Id.Should().Be(BOS);
        player.LastTeam!.Id.Should().Be(TOR);
        player.LastSeasonStartYear.Should().Be(2026);
        player.SweaterNumber.Should().Be(34);
        player.IsInjuredReserve.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetPlayer_OnInjuredReserve_Marked()
    {
        await FreshReport();

        var player = await Repository().GetPlayer(INJURED);

        player!.CurrentTeam!.Id.Should().Be(TOR);
        player.IsInjuredReserve.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetPlayer_OffEveryRoster_NoCurrentTeam_LastTeamKept()
    {
        await FreshReport();

        var sentDown = await Repository().GetPlayer(SENT_DOWN);
        var retired = await Repository().GetPlayer(RETIRED);

        sentDown!.CurrentTeam.Should().BeNull();
        sentDown.LastTeam!.Id.Should().Be(TOR);
        retired!.CurrentTeam.Should().BeNull();
        retired.LastTeam!.Id.Should().Be(TOR);
        retired.LastSeasonStartYear.Should().Be(2011);
    }

    [TestMethod]
    public async Task GetPlayer_StaleReport_FallsBackToThisSeasonsLatestGame()
    {
        await Report(Now.AddDays(-10), (TRADED, BOS, false));

        var traded = await Repository().GetPlayer(TRADED);
        var retired = await Repository().GetPlayer(RETIRED);

        traded!.CurrentTeam!.Id.Should().Be(TOR);
        retired!.CurrentTeam.Should().BeNull();
    }

    [TestMethod]
    public async Task GetTeamRoster_CurrentSeason_IsTheRosterReport()
    {
        await FreshReport();

        var roster = await Repository().GetTeamRoster(TOR, 2026);

        roster.Select(p => p.Id).Should().BeEquivalentTo(new[] { INJURED, GOALIE, CALLED_UP });
        roster.Single(p => p.Id == INJURED).IsInjuredReserve.Should().BeTrue();
        roster.Single(p => p.Id == CALLED_UP).GamesPlayed.Should().Be(0);
        roster.Single(p => p.Id == GOALIE).GamesPlayed.Should().Be(1);
    }

    [TestMethod]
    public async Task GetTeamRoster_PastSeason_PlayersWhoseLatestGameWasForTheTeam()
    {
        await FreshReport();

        var roster = await Repository().GetTeamRoster(TOR, 2025);

        roster.Select(p => p.Id).Should().Equal(CALLED_UP);
    }

    [TestMethod]
    public async Task GetTeamRoster_NoRecentReport_GameBased()
    {
        var roster = await Repository().GetTeamRoster(TOR, 2026);

        roster.Select(p => p.Id).Should().BeEquivalentTo(new[] { TRADED, INJURED, SENT_DOWN, GOALIE });
    }

    [TestMethod]
    public async Task GetGamePlayers_Upcoming_LeavesOutInjuredReserve()
    {
        await FreshReport();
        Game(2026020100, 2026, new DateTime(2026, 11, 8, 23, 0, 0), played: false);
        await _db.SaveChangesAsync();

        var players = await Repository().GetGamePlayers(2026020100);

        players!.Roster.Select(p => p.Id).Should().Contain(GOALIE).And.NotContain(INJURED);
    }

    [TestMethod]
    public async Task GetLatestTeams_CurrentAndLastTeams()
    {
        await FreshReport();

        var teams = (await Repository().GetLatestTeams([TRADED, SENT_DOWN, NO_GAMES])).ToDictionary(t => t.PlayerId);

        teams.Keys.Should().BeEquivalentTo(new[] { TRADED, SENT_DOWN });
        teams[TRADED].TeamAbbreviation.Should().Be("TOR");
        teams[TRADED].CurrentTeamAbbreviation.Should().Be("BOS");
        teams[SENT_DOWN].CurrentTeamId.Should().BeNull();
    }

    [TestMethod]
    public async Task GetPlayersLastGames_EveryPlayerWithAGame_BackupGamesDontCount()
    {
        var players = (await Repository().GetPlayersLastGames()).ToDictionary(p => p.Id, p => p.LastGameDateUTC);

        players.Keys.Should().BeEquivalentTo(new[] { TRADED, INJURED, SENT_DOWN, RETIRED, GOALIE, CALLED_UP });
        players[RETIRED].Should().Be(new DateTime(2012, 4, 7, 23, 0, 0));
        players[GOALIE].Should().Be(new DateTime(2026, 10, 10, 23, 0, 0));
        players[TRADED].Kind.Should().Be(DateTimeKind.Utc);
    }
}
