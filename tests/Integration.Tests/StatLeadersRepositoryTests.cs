using DatabaseAccess.WebPlayerRepository;
using Entities.DbModels;
using Entities.Types;

namespace Integration.Tests;

/// <summary>WebPlayerRepository.GetSeasonTotals (#182): the stat leaders' per-player sums run in Postgres.</summary>
[TestClass]
[TestCategory(TestDatabase.Category)]
public class StatLeadersRepositoryTests
{
    private const int G1 = Seed.Season * 1_000_000 + 20_001;
    private const int G2 = G1 + 1;
    private const int Playoff = Seed.Season * 1_000_000 + 30_111;
    private const int Matthews = 8479318;
    private const int Marner = 8478483;
    private const int Woll = 8479361;
    private const int Stolarz = 8476932;
    private const int Swayman = 8480280;
    private static readonly DateTime Start = new(2026, 10, 10, 23, 0, 0);

    [TestInitialize]
    public async Task Setup()
    {
        await TestDatabase.Reset();
        await Seed.DefaultTeams();
        await Seed.Games(
            new Seed.Game(G1, Start) { Played = true },
            new Seed.Game(G2, Start.AddDays(2)) { Played = true },
            new Seed.Game(Playoff, Start.AddMonths(7)) { Played = true, GameType = 3 });

        await using var db = TestContexts.Web();
        foreach (var (id, first, last) in new[]
                 {
                     (Matthews, "Auston", "Matthews"), (Marner, "Mitch", "Marner"), (Woll, "Joseph", "Woll"),
                     (Stolarz, "Anthony", "Stolarz"), (Swayman, "Jeremy", "Swayman"),
                 })
            db.Player.Add(new DbPlayer { Id = id, FirstName = first, LastName = last, HeadShot = $"{id}.png", CurrentTeamId = Seed.Leafs, BirthDate = new DateTime(1997, 9, 17) });
        db.GameSkaterStats.AddRange(
            Skater(G1, Matthews, Seed.Leafs, goals: 2, assists: 1, toi: 1200),
            Skater(G2, Matthews, Seed.Leafs, goals: 1, assists: 0, toi: 1100),
            Skater(Playoff, Matthews, Seed.Leafs, goals: 5, assists: 5, toi: 1300),  // playoffs don't count
            Skater(G1, Marner, Seed.Leafs, goals: 0, assists: 2, toi: 1000),
            Skater(G2, Marner, Seed.Bruins, goals: 0, assists: 0, toi: 0, position: POSITION.RightWing));  // traded, scratched
        db.GameGoalieStats.AddRange(
            GoalieLine(G1, Woll, Seed.Leafs, saves: 30, goalsAgainst: 0, toi: 3600, starter: true),      // shutout
            GoalieLine(G2, Woll, Seed.Leafs, saves: 10, goalsAgainst: 0, toi: 1800, starter: true),      // shared: no shutout
            GoalieLine(G2, Stolarz, Seed.Leafs, saves: 12, goalsAgainst: 2, toi: 1800, starter: false),
            GoalieLine(G1, Swayman, Seed.Bruins, saves: 20, goalsAgainst: 3, toi: 3600, starter: true));
        await db.SaveChangesAsync();
    }

    private static DbGameSkaterStats Skater(int gameId, int playerId, int teamId, int goals, int assists, int toi,
        POSITION position = POSITION.Center) => new()
    {
        GameId = gameId, PlayerId = playerId, TeamId = teamId, Goals = goals, Assists = assists,
        TimeOnIceSeconds = toi, ShotsOnGoal = goals * 3, Hits = 1, Position = position,
    };

    private static DbGameGoalieStats GoalieLine(int gameId, int playerId, int teamId, int saves, int goalsAgainst, int toi,
        bool starter) => new()
    {
        GameId = gameId, PlayerId = playerId, TeamId = teamId, EvenStrengthShotsSaved = saves,
        EvenStrengthGoalsAllowed = goalsAgainst, TimeOnIceSeconds = toi, IsStarter = starter,
    };

    [TestMethod]
    public async Task GetSeasonTotals_ShouldSumTheRegularSeason()
    {
        await using var db = TestContexts.Web();

        var totals = (await new WebPlayerRepository(db).GetSeasonTotals(Seed.Season)).ToDictionary(t => t.PlayerId);

        var matthews = totals[Matthews];
        matthews.IsGoalie.Should().BeFalse();
        matthews.GamesPlayed.Should().Be(2);
        (matthews.Goals, matthews.Assists, matthews.Shots, matthews.Hits).Should().Be((3, 1, 9, 2));
        matthews.TimeOnIceSeconds.Should().Be(2300);
        matthews.TeamAbbreviation.Should().Be("TOR");
        matthews.HeadShot.Should().Be($"{Matthews}.png");

        // Team and position from the latest game; a game without ice time isn't a game played
        var marner = totals[Marner];
        marner.GamesPlayed.Should().Be(1);
        marner.TeamId.Should().Be(Seed.Bruins);
        marner.Position.Should().Be(POSITION.RightWing);
    }

    [TestMethod]
    public async Task GetSeasonTotals_ShouldCountGoalieShutoutsOnlyWhenAlone()
    {
        await using var db = TestContexts.Web();

        var totals = (await new WebPlayerRepository(db).GetSeasonTotals(Seed.Season)).ToDictionary(t => t.PlayerId);

        var woll = totals[Woll];
        woll.IsGoalie.Should().BeTrue();
        woll.Position.Should().Be(POSITION.Goalie);
        (woll.GamesPlayed, woll.GamesStarted, woll.Saves, woll.GoalsAgainst, woll.TimeOnIceSeconds).Should().Be((2, 2, 40, 0, 5400));
        woll.Shutouts.Should().Be(1);
        totals[Stolarz].Shutouts.Should().Be(0);
        totals[Swayman].TeamAbbreviation.Should().Be("BOS");
    }

    [TestMethod]
    public async Task GetSeasonTotals_OtherSeason_ShouldBeEmpty()
    {
        await using var db = TestContexts.Web();

        (await new WebPlayerRepository(db).GetSeasonTotals(Seed.Season - 1)).Should().BeEmpty();
    }
}
