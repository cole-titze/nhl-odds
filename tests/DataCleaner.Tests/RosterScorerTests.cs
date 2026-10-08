using Entities.DbModels;
using Entities.Types;

namespace DataCleaner.Tests;

public class RosterScorerTests
{
    private const int TEAM = 1;
    private const int OPPONENT = 2;
    private static readonly DateTime START = new(2024, 10, 10, 23, 0, 0);
    // Test game ids have no season prefix, so save percentages shrink toward the default league average
    private const double PRIOR = 0.91;

    private static double Shrunk(int saves, int shots) => (saves + 1000 * PRIOR) / (shots + 1000);

    private static DbGameRaw Game(int id, DateTime date, int home = TEAM, int away = OPPONENT) =>
        new() { Id = id, GameDateUTC = date, HomeTeamId = home, AwayTeamId = away };

    private static DbGameSkaterStats Skater(int gameId, int playerId, int goals, int teamId = TEAM) => new()
    {
        GameId = gameId,
        PlayerId = playerId,
        TeamId = teamId,
        Goals = goals,
        ShotsOnGoal = 3,
        TimeOnIceSeconds = 1200,
        Position = POSITION.LeftWing,
    };

    private static DbGameGoalieStats Goalie(int gameId, int playerId, bool isStarter, int saved, int allowed, int teamId = TEAM) => new()
    {
        GameId = gameId,
        PlayerId = playerId,
        TeamId = teamId,
        EvenStrengthShotsSaved = isStarter ? saved : 0,
        EvenStrengthGoalsAllowed = isStarter ? allowed : 0,
        TimeOnIceSeconds = isStarter ? 3600 : 0,
        IsStarter = isStarter,
    };

    [Fact]
    public void GetTeamRosterValues_SameBeforeAndAfterGameIsPlayed()
    {
        var games = new[] { Game(1, START), Game(2, START.AddDays(2)), Game(3, START.AddDays(4)) };
        var skaters = new List<DbGameSkaterStats>
        {
            Skater(1, 10, 1), Skater(1, 11, 0),
            Skater(2, 10, 2), Skater(2, 12, 1),
        };
        var goalies = new List<DbGameGoalieStats>
        {
            Goalie(1, 30, true, 25, 2), Goalie(1, 31, false, 0, 0),
            Goalie(2, 31, true, 28, 3), Goalie(2, 30, false, 0, 0),
        };
        var before = new RosterScorer(skaters, goalies, games).GetTeamRosterValues(3, TEAM);

        // Game 3 gets played with a different lineup and the other goalie starting
        skaters.AddRange([Skater(3, 13, 3), Skater(3, 14, 2)]);
        goalies.AddRange([Goalie(3, 30, true, 40, 0), Goalie(3, 31, false, 0, 0)]);
        var after = new RosterScorer(skaters, goalies, games).GetTeamRosterValues(3, TEAM);

        Assert.Equal(before, after);
        Assert.NotEqual(0, before.RosterOffenseValue);
        Assert.NotEqual(0, before.RosterGoalieValue);
    }

    [Fact]
    public void GetTeamRosterValues_UsesPreviousGameLineup()
    {
        var games = new[] { Game(1, START), Game(2, START.AddDays(2)), Game(3, START.AddDays(4)) };
        var skaters = new[]
        {
            // Player 10 dressed in game 1 only, player 11 in game 2 only
            Skater(1, 10, 3),
            Skater(2, 11, 1),
        };
        var scorer = new RosterScorer(skaters, [], games);

        var values = scorer.GetTeamRosterValues(3, TEAM);

        // Only player 11 counts: (0.63*0 + 0.75*1 + 0.075*3) per 20 minutes -> per 60
        Assert.Equal((0.75 + 0.075 * 3) * 3, values.RosterOffenseValue, 6);
    }

    [Fact]
    public void GetTeamRosterValues_RatesWeightedByIceTime()
    {
        var games = new[] { Game(1, START), Game(2, START.AddDays(2)), Game(3, START.AddDays(4)) };
        var shortShift = Skater(1, 10, 1);
        shortShift.ShotsOnGoal = 0;
        shortShift.TimeOnIceSeconds = 6;
        var fullGame = Skater(2, 10, 0);
        fullGame.ShotsOnGoal = 0;
        var scorer = new RosterScorer([shortShift, fullGame], [], games);

        var values = scorer.GetTeamRosterValues(3, TEAM);

        // One goal over 1206 seconds, not the average of a 600-goals-per-60 game and a 0 game
        Assert.Equal(0.75 / (1206 / 3600.0), values.RosterOffenseValue, 6);
    }

    [Fact]
    public void GetTeamRosterValues_TooLittleIceTime_Skipped()
    {
        var games = new[] { Game(1, START), Game(2, START.AddDays(2)) };
        var shortShift = Skater(1, 10, 1);
        shortShift.TimeOnIceSeconds = 30;
        var scorer = new RosterScorer([shortShift], [], games);

        Assert.Equal(0, scorer.GetTeamRosterValues(2, TEAM).RosterOffenseValue);
    }

    [Fact]
    public void GetTeamRosterValues_NoPriorGames_ReturnsZero()
    {
        var scorer = new RosterScorer([Skater(1, 10, 1)], [Goalie(1, 30, true, 25, 2)], [Game(1, START)]);

        var values = scorer.GetTeamRosterValues(1, TEAM);

        Assert.Equal(new TeamRosterValues(0, 0, PRIOR, 0, 0, PRIOR), values);
    }

    [Fact]
    public void GetTeamRosterValues_GoalieWeightedByRecentStartShare()
    {
        // Goalie 30 (.900) starts 3 of 4, goalie 31 (.800) starts 1 of 4; games 3 days apart
        var games = Enumerable.Range(1, 5).Select(i => Game(i, START.AddDays(3 * i))).ToList();
        var goalies = new[]
        {
            Goalie(1, 30, true, 90, 10), Goalie(2, 30, true, 90, 10), Goalie(3, 31, true, 80, 20), Goalie(4, 30, true, 90, 10),
        };
        var scorer = new RosterScorer([], goalies, games);

        var values = scorer.GetTeamRosterValues(5, TEAM);

        Assert.Equal(0.75 * Shrunk(270, 300) + 0.25 * Shrunk(80, 100), values.RosterGoalieValue, 6);
    }

    [Fact]
    public void GetTeamRosterValues_BackToBack_FavorsOtherGoalie()
    {
        // Same starts as above, but game 5 is the night after game 4 (started by goalie 30)
        var games = Enumerable.Range(1, 4).Select(i => Game(i, START.AddDays(3 * i))).ToList();
        games.Add(Game(5, START.AddDays(12).AddHours(24)));
        var goalies = new[]
        {
            Goalie(1, 30, true, 90, 10), Goalie(2, 30, true, 90, 10), Goalie(3, 31, true, 80, 20), Goalie(4, 30, true, 90, 10),
        };
        var scorer = new RosterScorer([], goalies, games);

        var values = scorer.GetTeamRosterValues(5, TEAM);

        Assert.Equal(0.1 * Shrunk(270, 300) + 0.9 * Shrunk(80, 100), values.RosterGoalieValue, 6);
    }

    [Fact]
    public void GetTeamRosterValues_IgnoresGoalieWhoLeftTeam()
    {
        // Goalie 31 started game 2 for TEAM, then appeared for OPPONENT in game 3
        var games = new[]
        {
            Game(1, START), Game(2, START.AddDays(3)),
            Game(3, START.AddDays(5), home: OPPONENT, away: 3),
            Game(4, START.AddDays(7)),
        };
        var goalies = new[]
        {
            Goalie(1, 30, true, 90, 10), Goalie(2, 31, true, 80, 20),
            Goalie(3, 31, true, 70, 30, teamId: OPPONENT),
        };
        var scorer = new RosterScorer([], goalies, games);

        var values = scorer.GetTeamRosterValues(4, TEAM);

        Assert.Equal(Shrunk(90, 100), values.RosterGoalieValue, 6);
    }

    [Fact]
    public void GetTeamSavePct_UsesGoalieShotsAndResetsSeason()
    {
        // Season 2023 game, then two 2024 games (one with a relief goalie), then the game being scored
        var games = new[]
        {
            Game(2023020001, START.AddYears(-1)),
            Game(2024020001, START), Game(2024020002, START.AddDays(2)), Game(2024020003, START.AddDays(4)),
        };
        var goalies = new[]
        {
            Goalie(2023020001, 30, true, 20, 10),
            Goalie(2024020001, 30, true, 27, 3),
            Goalie(2024020002, 30, true, 15, 5), Goalie(2024020002, 31, false, 0, 0),
        };
        // Relief goalie: not the starter, but his shots count toward the team
        goalies[3].EvenStrengthShotsSaved = 9;
        goalies[3].EvenStrengthGoalsAllowed = 1;
        var scorer = new RosterScorer([], goalies, games);

        var (season, recent) = scorer.GetTeamSavePct(2024020003, TEAM);

        // Prior is the 2023 league average (20 of 30); the season total leaves out the 2023 game
        var prior = 20.0 / 30;
        Assert.Equal((51 + 1000 * prior) / (60 + 1000), season, 6);
        Assert.Equal((71 + 1000 * prior) / (90 + 1000), recent, 6);
    }

    [Fact]
    public void GetTeamSavePct_NoGames_ReturnsPrior()
    {
        var scorer = new RosterScorer([], [], [Game(1, START)]);

        Assert.Equal((PRIOR, PRIOR), scorer.GetTeamSavePct(1, TEAM));
    }

    private static DbRosterStatus Status(DateTime snapshot, int playerId, bool isInjuredReserve, int teamId = TEAM) => new()
    {
        SnapshotUTC = snapshot,
        PlayerId = playerId,
        TeamId = teamId,
        IsInjuredReserve = isInjuredReserve,
    };

    [Fact]
    public void GetTeamRosterValues_InjuredSkaterReplacedByLatestHealthyPlayer()
    {
        var games = new[] { Game(1, START), Game(2, START.AddDays(2)), Game(3, START.AddDays(4)), Game(4, START.AddDays(6)) };
        var skaters = new[]
        {
            // Player 12 last played in game 1, player 13 in game 2; game 3's lineup is players 10 and 11
            Skater(1, 12, 0), Skater(2, 13, 2),
            Skater(3, 10, 3), Skater(3, 11, 1),
        };
        var snapshot = START.AddDays(6).AddHours(-12);
        var statuses = new[] { Status(snapshot, 10, true), Status(snapshot, 11, false), Status(snapshot, 12, false), Status(snapshot, 13, false) };

        var values = new RosterScorer(skaters, [], games, statuses).GetTeamRosterValues(4, TEAM);

        // Player 10 (IR) is swapped for player 13, the healthy forward who played most recently (not player 12):
        // player 11 (1 goal, 3 shots) + player 13 (2 goals, 3 shots), each per 20 minutes -> per 60
        Assert.Equal((0.75 * 1 + 0.075 * 3) * 3 + (0.75 * 2 + 0.075 * 3) * 3, values.RosterOffenseValue, 6);
    }

    [Fact]
    public void GetTeamRosterValues_InjuredDefensemanNotReplacedByForward()
    {
        var games = new[] { Game(1, START), Game(2, START.AddDays(2)), Game(3, START.AddDays(4)) };
        var defenseman = Skater(2, 10, 1);
        defenseman.Position = POSITION.Defenseman;
        var skaters = new[] { Skater(1, 12, 3), defenseman };
        var snapshot = START.AddDays(4).AddHours(-12);
        var statuses = new[] { Status(snapshot, 10, true), Status(snapshot, 12, false) };

        var values = new RosterScorer(skaters, [], games, statuses).GetTeamRosterValues(3, TEAM);

        Assert.Equal(0, values.RosterOffenseValue);
    }

    [Fact]
    public void GetTeamRosterValues_IgnoresOldOrLaterSnapshots()
    {
        var games = new[] { Game(1, START), Game(2, START.AddDays(5)) };
        var skaters = new[] { Skater(1, 10, 1) };
        var before = new RosterScorer(skaters, [], games).GetTeamRosterValues(2, TEAM);

        // Three days before the game is too old; after puck drop wasn't known yet
        var statuses = new[] { Status(START.AddDays(2), 10, true), Status(START.AddDays(5).AddHours(1), 10, true) };
        var after = new RosterScorer(skaters, [], games, statuses).GetTeamRosterValues(2, TEAM);

        Assert.Equal(before, after);
        Assert.NotEqual(0, after.RosterOffenseValue);
    }

    [Fact]
    public void GetTeamRosterValues_InjuredGoalieDropped()
    {
        // Same starts as the start-share test: goalie 30 (.900) 3 of 4, goalie 31 (.800) 1 of 4; 30 goes on IR
        var games = Enumerable.Range(1, 5).Select(i => Game(i, START.AddDays(3 * i))).ToList();
        var goalies = new[]
        {
            Goalie(1, 30, true, 90, 10), Goalie(2, 30, true, 90, 10), Goalie(3, 31, true, 80, 20), Goalie(4, 30, true, 90, 10),
        };
        var snapshot = START.AddDays(15).AddHours(-12);
        var statuses = new[] { Status(snapshot, 30, true), Status(snapshot, 31, false) };

        var values = new RosterScorer([], goalies, games, statuses).GetTeamRosterValues(5, TEAM);

        Assert.Equal(Shrunk(80, 100), values.RosterGoalieValue, 6);
    }

    [Fact]
    public void GetTeamRosterValues_AllStartersInjured_UsesHealthyGoalies()
    {
        // Goalie 30 started every game; goalie 31 dressed as the backup in game 1 after starting game 0
        var games = Enumerable.Range(0, 4).Select(i => Game(i, START.AddDays(3 * i))).ToList();
        var goalies = new[]
        {
            Goalie(0, 31, true, 80, 20),
            Goalie(1, 30, true, 90, 10), Goalie(1, 31, false, 0, 0),
        };
        var lastStarts = Enumerable.Range(2, 1).Select(i => Goalie(i, 30, true, 90, 10));
        var snapshot = START.AddDays(9).AddHours(-12);
        var statuses = new[] { Status(snapshot, 30, true), Status(snapshot, 31, false) };

        var values = new RosterScorer([], goalies.Concat(lastStarts), games, statuses).GetTeamRosterValues(3, TEAM);

        Assert.Equal(Shrunk(80, 100), values.RosterGoalieValue, 6);
    }
}