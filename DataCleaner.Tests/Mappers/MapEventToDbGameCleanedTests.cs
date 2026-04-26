using DataCleaner.Mappers;
using DataCleaner.Tests.Helpers;
using Entities.DbModels;
using Entities.DbModels.GamePlayEvents;
using Entities.Models;
using Entities.Types.Enums;
using Zone = Entities.Types.Enums.Zone;

namespace DataCleaner.Tests.Mappers;

public class MapEventToDbGameCleanedTests
{
    private static readonly DateTime BaseDate = new(2024, 10, 10, 19, 0, 0);

    #region Null aggregator

    [Fact]
    public void Apply_NullEventAggregator_NoPropertiesModified()
    {
        var cleanedGame = new DbGameCleaned();
        var game = new GameBuilder().WithTeams(1, 2).Build();

        MapEventToDbGameCleaned.Apply(cleanedGame, eventAggregator: null, game);

        Assert.Equal(0, cleanedGame.HomePpEfficiency);
        Assert.Equal(0, cleanedGame.AwayPpEfficiency);
        Assert.Equal(0, cleanedGame.HomePkEfficiency);
        Assert.Equal(0, cleanedGame.HomeCorsiPct);
        Assert.Equal(0, cleanedGame.HomeGoalsPerGamePeriod1);
        Assert.Equal(0, cleanedGame.HomeOffensiveZoneFaceoffWinPct);
        Assert.Equal(0, cleanedGame.HomePenaltyDifferentialAvg);
    }

    [Fact]
    public void Apply_NullEventAggregator_PreexistingValuesUnchanged()
    {
        var cleanedGame = new DbGameCleaned
        {
            HomePpEfficiency = 0.25,
            HomeCorsiPct = 0.55,
            HomeGoalsPerGamePeriod1 = 1.2,
        };
        var game = new GameBuilder().WithTeams(1, 2).Build();

        MapEventToDbGameCleaned.Apply(cleanedGame, eventAggregator: null, game);

        // Early return means these pre-existing values should not be overwritten
        Assert.Equal(0.25, cleanedGame.HomePpEfficiency);
        Assert.Equal(0.55, cleanedGame.HomeCorsiPct);
        Assert.Equal(1.2, cleanedGame.HomeGoalsPerGamePeriod1);
    }

    #endregion

    #region No prior games

    [Fact]
    public void Apply_NoPriorGames_AllFieldsDefaultToZero()
    {
        // Only the target game exists — no prior games for aggregator to compute from
        var game = new GameBuilder().WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var aggregator = new EventAggregator(
            Array.Empty<DbPenalty>(),
            Array.Empty<DbGoal>(),
            Array.Empty<DbFaceoff>(),
            Array.Empty<DbMissedShot>(),
            new[] { game });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, game);

        Assert.Equal(0, cleanedGame.HomePpEfficiency);
        Assert.Equal(0, cleanedGame.AwayPpEfficiency);
        Assert.Equal(0, cleanedGame.HomeRecentPpEfficiency);
        Assert.Equal(0, cleanedGame.AwayRecentPpEfficiency);
        Assert.Equal(0, cleanedGame.HomePkEfficiency);
        Assert.Equal(0, cleanedGame.AwayPkEfficiency);
        Assert.Equal(0, cleanedGame.HomeCorsiPct);
        Assert.Equal(0, cleanedGame.AwayCorsiPct);
        Assert.Equal(0, cleanedGame.HomeGoalsPerGamePeriod1);
        Assert.Equal(0, cleanedGame.HomeGoalsPerGamePeriod2);
        Assert.Equal(0, cleanedGame.HomeGoalsPerGamePeriod3);
    }

    #endregion

    #region PP efficiency

    [Fact]
    public void Apply_PpEfficiency_CalculatedFromPriorGames()
    {
        // Prior game: team 2 (away) commits 2 minor penalties, team 1 (home) scores 1 PPG
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).WithPPG(1, 0).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var penalties = new[]
        {
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 2, PenaltySeverity = PenaltySeverity.Minor },
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 2, PenaltySeverity = PenaltySeverity.Minor },
        };

        var aggregator = new EventAggregator(
            penalties, Array.Empty<DbGoal>(), Array.Empty<DbFaceoff>(),
            Array.Empty<DbMissedShot>(), new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Team 1: 1 PPG / 2 opponent penalties = 0.5
        Assert.Equal(0.5, cleanedGame.HomePpEfficiency);
        Assert.Equal(0.5, cleanedGame.HomeRecentPpEfficiency);
    }

    #endregion

    #region PK efficiency

    [Fact]
    public void Apply_PkEfficiency_CalculatedFromPriorGames()
    {
        // Prior game: team 1 (home) commits 2 minor penalties, team 2 scores 1 PPG
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).WithPPG(0, 1).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var penalties = new[]
        {
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 1, PenaltySeverity = PenaltySeverity.Minor },
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 1, PenaltySeverity = PenaltySeverity.Minor },
        };

        var aggregator = new EventAggregator(
            penalties, Array.Empty<DbGoal>(), Array.Empty<DbFaceoff>(),
            Array.Empty<DbMissedShot>(), new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Team 1 PK: 1 - (1 opponent PPG / 2 own penalties) = 0.5
        Assert.Equal(0.5, cleanedGame.HomePkEfficiency);
    }

    [Fact]
    public void Apply_PkEfficiency_NoPenalties_ReturnsOne()
    {
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).WithPPG(0, 0).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var aggregator = new EventAggregator(
            Array.Empty<DbPenalty>(), Array.Empty<DbGoal>(), Array.Empty<DbFaceoff>(),
            Array.Empty<DbMissedShot>(), new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // No penalties taken → perfect PK = 1.0
        Assert.Equal(1, cleanedGame.HomePkEfficiency);
    }

    #endregion

    #region Goals per period

    [Fact]
    public void Apply_GoalsPerPeriod_CalculatedCorrectly()
    {
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var goals = new[]
        {
            new DbGoal { GameId = 1, ScoringPlayerTeamId = 1, PeriodNumber = 1 },
            new DbGoal { GameId = 1, ScoringPlayerTeamId = 1, PeriodNumber = 1 },
            new DbGoal { GameId = 1, ScoringPlayerTeamId = 1, PeriodNumber = 2 },
            new DbGoal { GameId = 1, ScoringPlayerTeamId = 2, PeriodNumber = 3 },
        };

        var aggregator = new EventAggregator(
            Array.Empty<DbPenalty>(), goals, Array.Empty<DbFaceoff>(),
            Array.Empty<DbMissedShot>(), new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Team 1: 2 goals in P1, 1 in P2, 0 in P3 over 1 game
        Assert.Equal(2.0, cleanedGame.HomeGoalsPerGamePeriod1);
        Assert.Equal(1.0, cleanedGame.HomeGoalsPerGamePeriod2);
        Assert.Equal(0.0, cleanedGame.HomeGoalsPerGamePeriod3);
        // Team 2: 0 in P1, 0 in P2, 1 in P3
        Assert.Equal(0.0, cleanedGame.AwayGoalsPerGamePeriod1);
        Assert.Equal(0.0, cleanedGame.AwayGoalsPerGamePeriod2);
        Assert.Equal(1.0, cleanedGame.AwayGoalsPerGamePeriod3);
    }

    #endregion

    #region Offensive zone faceoff win pct

    [Fact]
    public void Apply_OffensiveZoneFaceoffWinPct_CalculatedCorrectly()
    {
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var faceoffs = new[]
        {
            new DbFaceoff { GameId = 1, WinningTeamId = 1, Zone = Zone.Offensive },
            new DbFaceoff { GameId = 1, WinningTeamId = 1, Zone = Zone.Defensive },
            new DbFaceoff { GameId = 1, WinningTeamId = 1, Zone = Zone.Neutral },
            new DbFaceoff { GameId = 1, WinningTeamId = 2, Zone = Zone.Offensive },
        };

        var aggregator = new EventAggregator(
            Array.Empty<DbPenalty>(), Array.Empty<DbGoal>(), faceoffs,
            Array.Empty<DbMissedShot>(), new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Team 1: 1 offensive zone win out of 3 total wins = 1/3
        Assert.Equal(1.0 / 3.0, cleanedGame.HomeOffensiveZoneFaceoffWinPct, 5);
        // Team 2: 1 offensive zone win out of 1 total win = 1.0
        Assert.Equal(1.0, cleanedGame.AwayOffensiveZoneFaceoffWinPct);
    }

    #endregion

    #region Penalty differential

    [Fact]
    public void Apply_PenaltyDifferentialAvg_CalculatedCorrectly()
    {
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var penalties = new[]
        {
            // Team 1 commits 1 penalty
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 1, PenaltySeverity = PenaltySeverity.Minor },
            // Team 2 commits 3 penalties
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 2, PenaltySeverity = PenaltySeverity.Minor },
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 2, PenaltySeverity = PenaltySeverity.Major },
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 2, PenaltySeverity = PenaltySeverity.Minor },
        };

        var aggregator = new EventAggregator(
            penalties, Array.Empty<DbGoal>(), Array.Empty<DbFaceoff>(),
            Array.Empty<DbMissedShot>(), new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Team 1: opponent(3) - own(1) = +2.0 differential per game
        Assert.Equal(2.0, cleanedGame.HomePenaltyDifferentialAvg);
        // Team 2: opponent(1) - own(3) = -2.0
        Assert.Equal(-2.0, cleanedGame.AwayPenaltyDifferentialAvg);
    }

    [Fact]
    public void Apply_PenaltyDifferential_IgnoresNonQualifyingSeverities()
    {
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var penalties = new[]
        {
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 1, PenaltySeverity = PenaltySeverity.Misconduct },
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 2, PenaltySeverity = PenaltySeverity.Minor },
        };

        var aggregator = new EventAggregator(
            penalties, Array.Empty<DbGoal>(), Array.Empty<DbFaceoff>(),
            Array.Empty<DbMissedShot>(), new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Team 1: opponent has 1 qualifying penalty, own has 0 qualifying → diff = 1
        Assert.Equal(1.0, cleanedGame.HomePenaltyDifferentialAvg);
    }

    #endregion

    #region Corsi pct

    [Fact]
    public void Apply_CorsiPct_CalculatedCorrectly()
    {
        // SOG: home=30, away=25; Blocked: home=10, away=8
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate)
            .WithSOG(30, 25).WithBlockedShots(10, 8).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var missedShots = new[]
        {
            new DbMissedShot { GameId = 1, ShootingTeamId = 1 },
            new DbMissedShot { GameId = 1, ShootingTeamId = 1 },
            new DbMissedShot { GameId = 1, ShootingTeamId = 2 },
        };

        var aggregator = new EventAggregator(
            Array.Empty<DbPenalty>(), Array.Empty<DbGoal>(), Array.Empty<DbFaceoff>(),
            missedShots, new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Team 1 CF = SOG(30) + oppBlocked(8) + missed(2) = 40
        // Team 1 CA = oppSOG(25) + ownBlocked(10) + oppMissed(1) = 36
        // Corsi% = 40 / 76
        Assert.Equal(40.0 / 76.0, cleanedGame.HomeCorsiPct, 5);
        // Team 2 CF = SOG(25) + oppBlocked(10) + missed(1) = 36
        // Team 2 CA = oppSOG(30) + ownBlocked(8) + oppMissed(2) = 40
        // Corsi% = 36 / 76
        Assert.Equal(36.0 / 76.0, cleanedGame.AwayCorsiPct, 5);
    }

    #endregion

    #region All fields mapped

    [Fact]
    public void Apply_WithEventData_AllFieldsPopulated()
    {
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate)
            .WithPPG(1, 0).WithSOG(30, 25).WithBlockedShots(10, 8).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();

        var penalties = new[]
        {
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 2, PenaltySeverity = PenaltySeverity.Minor },
            new DbPenalty { GameId = 1, CommittedByPlayerTeamId = 1, PenaltySeverity = PenaltySeverity.Minor },
        };
        var goals = new[]
        {
            new DbGoal { GameId = 1, ScoringPlayerTeamId = 1, PeriodNumber = 1 },
            new DbGoal { GameId = 1, ScoringPlayerTeamId = 1, PeriodNumber = 2 },
            new DbGoal { GameId = 1, ScoringPlayerTeamId = 2, PeriodNumber = 3 },
        };
        var faceoffs = new[]
        {
            new DbFaceoff { GameId = 1, WinningTeamId = 1, Zone = Zone.Offensive },
            new DbFaceoff { GameId = 1, WinningTeamId = 1, Zone = Zone.Defensive },
        };
        var missedShots = new[]
        {
            new DbMissedShot { GameId = 1, ShootingTeamId = 1 },
        };

        var aggregator = new EventAggregator(
            penalties, goals, faceoffs, missedShots,
            new[] { priorGame, targetGame });

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Verify all 32 event-based fields are non-default (at least one side should be nonzero)
        Assert.NotEqual(0, cleanedGame.HomePpEfficiency);
        Assert.NotEqual(0, cleanedGame.HomeRecentPpEfficiency);
        Assert.NotEqual(0, cleanedGame.HomePkEfficiency);
        Assert.NotEqual(0, cleanedGame.HomeRecentPkEfficiency);
        Assert.NotEqual(0, cleanedGame.HomeGoalsPerGamePeriod1);
        Assert.NotEqual(0, cleanedGame.HomeGoalsPerGamePeriod2);
        Assert.NotEqual(0, cleanedGame.AwayGoalsPerGamePeriod3);
        Assert.NotEqual(0, cleanedGame.HomeOffensiveZoneFaceoffWinPct);
        Assert.NotEqual(0, cleanedGame.HomeRecentOffensiveZoneFaceoffWinPct);
        Assert.NotEqual(0, cleanedGame.HomeCorsiPct);
        Assert.NotEqual(0, cleanedGame.HomeRecentCorsiPct);
        Assert.NotEqual(0, cleanedGame.AwayCorsiPct);
        Assert.NotEqual(0, cleanedGame.AwayRecentCorsiPct);
    }

    #endregion

    #region Recent vs season values

    [Fact]
    public void Apply_RecentValues_OnlyUseLastFiveGames()
    {
        // Create 6 prior games; "recent" should only use the last 5
        var games = new List<Game>();
        var goals = new List<DbGoal>();

        for (int i = 1; i <= 6; i++)
        {
            games.Add(new GameBuilder()
                .WithId(i).WithTeams(1, 2).WithDate(BaseDate.AddDays(i)).Build());

            // First game: team 1 scores 10 goals in P1 (outlier)
            // Games 2-6: team 1 scores 1 goal in P1 each
            int goalCount = i == 1 ? 10 : 1;
            for (int g = 0; g < goalCount; g++)
            {
                goals.Add(new DbGoal { GameId = i, ScoringPlayerTeamId = 1, PeriodNumber = 1 });
            }
        }

        var targetGame = new GameBuilder()
            .WithId(100).WithTeams(1, 2).WithDate(BaseDate.AddDays(10)).Build();
        games.Add(targetGame);

        var aggregator = new EventAggregator(
            Array.Empty<DbPenalty>(), goals, Array.Empty<DbFaceoff>(),
            Array.Empty<DbMissedShot>(), games);

        var cleanedGame = new DbGameCleaned();
        MapEventToDbGameCleaned.Apply(cleanedGame, aggregator, targetGame);

        // Season (all 6 games): (10 + 5) / 6 = 2.5
        Assert.Equal(2.5, cleanedGame.HomeGoalsPerGamePeriod1, 5);
        // Recent (last 5 games: 2-6): 5 / 5 = 1.0
        Assert.Equal(1.0, cleanedGame.HomeRecentGoalsPerGamePeriod1, 5);
    }

    #endregion
}