using DataCleaner.Mappers;
using DataCleaner.Tests.Helpers;
using Entities.Models;
using Entities.Types;
using Entities.Types.Enums;

namespace DataCleaner.Tests.Mappers;

public class MapGameToDbGameCleanedTests
{
    private static readonly DateTime BaseDate = new(2024, 10, 10, 19, 0, 0);

    #region GetWinRatioOfGames

    [Fact]
    public void GetWinRatioOfGames_EmptyList_ReturnsZero()
    {
        var result = MapGameToDbGameCleaned.GetWinRatioOfGames([], teamId: 1);
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetWinRatioOfGames_AllWins_ReturnsOne()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
        };
        Assert.Equal(1.0, MapGameToDbGameCleaned.GetWinRatioOfGames(games, teamId: 1));
    }

    [Fact]
    public void GetWinRatioOfGames_AllLosses_ReturnsZero()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(),
        };
        Assert.Equal(0, MapGameToDbGameCleaned.GetWinRatioOfGames(games, teamId: 1));
    }

    [Fact]
    public void GetWinRatioOfGames_MixedResults_ReturnsCorrectRatio()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
        };
        Assert.Equal(0.6, MapGameToDbGameCleaned.GetWinRatioOfGames(games, teamId: 1));
    }

    [Fact]
    public void GetWinRatioOfGames_TeamIsAway_CountsCorrectly()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(2, 1).WithWinner(Winner.AWAY).Build(),
            new GameBuilder().WithTeams(2, 1).WithWinner(Winner.HOME).Build(),
        };
        // Team 1 is away, won 1 of 2
        Assert.Equal(0.5, MapGameToDbGameCleaned.GetWinRatioOfGames(games, teamId: 1));
    }

    #endregion

    #region GetStatAvg

    [Fact]
    public void GetStatAvg_EmptyList_ReturnsZero()
    {
        var result = MapGameToDbGameCleaned.GetStatAvg([], 1, g => g.HomeGoals, g => g.AwayGoals);
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetStatAvg_SingleGame_TeamIsHome_ReturnsHomeStat()
    {
        var games = new[] { new GameBuilder().WithTeams(1, 2).WithScore(4, 1).Build() };
        var result = MapGameToDbGameCleaned.GetStatAvg(games, 1, g => g.HomeGoals, g => g.AwayGoals);
        Assert.Equal(4.0, result);
    }

    [Fact]
    public void GetStatAvg_SingleGame_TeamIsAway_ReturnsAwayStat()
    {
        var games = new[] { new GameBuilder().WithTeams(2, 1).WithScore(4, 1).Build() };
        var result = MapGameToDbGameCleaned.GetStatAvg(games, 1, g => g.HomeGoals, g => g.AwayGoals);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void GetStatAvg_MultipleGames_ReturnsAverage()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithScore(3, 1).Build(),
            new GameBuilder().WithTeams(1, 2).WithScore(5, 2).Build(),
            new GameBuilder().WithTeams(1, 2).WithScore(4, 3).Build(),
        };
        // (3 + 5 + 4) / 3 = 4.0
        Assert.Equal(4.0, MapGameToDbGameCleaned.GetStatAvg(games, 1, g => g.HomeGoals, g => g.AwayGoals));
    }

    [Fact]
    public void GetStatAvg_MixedHomeAway_AveragesCorrectly()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithScore(3, 1).Build(),
            new GameBuilder().WithTeams(2, 1).WithScore(4, 2).Build(), // team 1 is away, scored 2
        };
        // Team 1: scored 3 (home) + 2 (away) = 5 / 2 = 2.5
        Assert.Equal(2.5, MapGameToDbGameCleaned.GetStatAvg(games, 1, g => g.HomeGoals, g => g.AwayGoals));
    }

    #endregion

    #region GetDoubleStatAvg

    [Fact]
    public void GetDoubleStatAvg_EmptyList_ReturnsZero()
    {
        var result = MapGameToDbGameCleaned.GetDoubleStatAvg([], 1,
            g => g.HomeFaceOffWinPercent, g => g.AwayFaceOffWinPercent);
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetDoubleStatAvg_ReturnsCorrectAvg()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithFaceOffPct(55.0, 45.0).Build(),
            new GameBuilder().WithTeams(1, 2).WithFaceOffPct(50.0, 50.0).Build(),
        };
        // (55 + 50) / 2 = 52.5
        Assert.Equal(52.5, MapGameToDbGameCleaned.GetDoubleStatAvg(games, 1,
            g => g.HomeFaceOffWinPercent, g => g.AwayFaceOffWinPercent));
    }

    #endregion

    #region GetSavePct

    [Fact]
    public void GetSavePct_EmptyList_ReturnsZero()
    {
        Assert.Equal(0, MapGameToDbGameCleaned.GetSavePct([], 1));
    }

    [Fact]
    public void GetSavePct_CalculatesCorrectly()
    {
        // Team 1 is home. Opponent (away) had 30 SOG and scored 3 goals.
        var games = new[] { new GameBuilder().WithTeams(1, 2).WithSOG(25, 30).WithScore(4, 3).Build() };
        // save% = (30 - 3) / 30 = 0.9
        Assert.Equal(0.9, MapGameToDbGameCleaned.GetSavePct(games, 1));
    }

    [Fact]
    public void GetSavePct_ZeroShotsAgainst_ReturnsZero()
    {
        var games = new[] { new GameBuilder().WithTeams(1, 2).WithSOG(30, 0).WithScore(5, 0).Build() };
        Assert.Equal(0, MapGameToDbGameCleaned.GetSavePct(games, 1));
    }

    [Fact]
    public void GetSavePct_PerfectSavePercentage()
    {
        var games = new[] { new GameBuilder().WithTeams(1, 2).WithSOG(25, 30).WithScore(3, 0).Build() };
        // (30 - 0) / 30 = 1.0
        Assert.Equal(1.0, MapGameToDbGameCleaned.GetSavePct(games, 1));
    }

    #endregion

    #region GetGoalDiffAvg

    [Fact]
    public void GetGoalDiffAvg_EmptyList_ReturnsZero()
    {
        Assert.Equal(0, MapGameToDbGameCleaned.GetGoalDiffAvg([], 1));
    }

    [Fact]
    public void GetGoalDiffAvg_PositiveDiff()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithScore(5, 2).Build(),
            new GameBuilder().WithTeams(1, 2).WithScore(3, 1).Build(),
        };
        // (3 + 2) / 2 = 2.5
        Assert.Equal(2.5, MapGameToDbGameCleaned.GetGoalDiffAvg(games, 1));
    }

    [Fact]
    public void GetGoalDiffAvg_NegativeDiff()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithScore(1, 4).Build(),
        };
        // 1 - 4 = -3
        Assert.Equal(-3.0, MapGameToDbGameCleaned.GetGoalDiffAvg(games, 1));
    }

    #endregion

    #region GetOvertimeRatio

    [Fact]
    public void GetOvertimeRatio_EmptyList_ReturnsZero()
    {
        Assert.Equal(0, MapGameToDbGameCleaned.GetOvertimeRatio([]));
    }

    [Fact]
    public void GetOvertimeRatio_AllRegulation_ReturnsZero()
    {
        var games = new[]
        {
            new GameBuilder().WithEndPeriod(PeriodType.Regulation).Build(),
            new GameBuilder().WithEndPeriod(PeriodType.Regulation).Build(),
        };
        Assert.Equal(0, MapGameToDbGameCleaned.GetOvertimeRatio(games));
    }

    [Fact]
    public void GetOvertimeRatio_MixedPeriods_ReturnsCorrectRatio()
    {
        var games = new[]
        {
            new GameBuilder().WithEndPeriod(PeriodType.Regulation).Build(),
            new GameBuilder().WithEndPeriod(PeriodType.Overtime).Build(),
            new GameBuilder().WithEndPeriod(PeriodType.Shootout).Build(),
            new GameBuilder().WithEndPeriod(PeriodType.Regulation).Build(),
        };
        // 2 non-regulation out of 4 = 0.5
        Assert.Equal(0.5, MapGameToDbGameCleaned.GetOvertimeRatio(games));
    }

    [Fact]
    public void GetOvertimeRatio_AllOvertime_ReturnsOne()
    {
        var games = new[]
        {
            new GameBuilder().WithEndPeriod(PeriodType.Overtime).Build(),
            new GameBuilder().WithEndPeriod(PeriodType.Shootout).Build(),
        };
        Assert.Equal(1.0, MapGameToDbGameCleaned.GetOvertimeRatio(games));
    }

    #endregion

    #region GetRegulationWinRatio

    [Fact]
    public void GetRegulationWinRatio_EmptyList_ReturnsZero()
    {
        Assert.Equal(0, MapGameToDbGameCleaned.GetRegulationWinRatio([], 1));
    }

    [Fact]
    public void GetRegulationWinRatio_OnlyCountsRegulationWins()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).WithEndPeriod(PeriodType.Regulation).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).WithEndPeriod(PeriodType.Overtime).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).WithEndPeriod(PeriodType.Shootout).Build(),
        };
        // Only 1 regulation win out of 3 total games
        Assert.Equal(1.0 / 3.0, MapGameToDbGameCleaned.GetRegulationWinRatio(games, 1), 5);
    }

    [Fact]
    public void GetRegulationWinRatio_LossesNotCounted()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).WithEndPeriod(PeriodType.Regulation).Build(),
        };
        Assert.Equal(0, MapGameToDbGameCleaned.GetRegulationWinRatio(games, 1));
    }

    #endregion

    #region GetStreak

    [Fact]
    public void GetStreak_EmptyList_ReturnsZero()
    {
        Assert.Equal(0, MapGameToDbGameCleaned.GetStreak([], 1));
    }

    [Fact]
    public void GetStreak_SingleWin_ReturnsOne()
    {
        var games = new[] { new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build() };
        Assert.Equal(1, MapGameToDbGameCleaned.GetStreak(games, 1));
    }

    [Fact]
    public void GetStreak_SingleLoss_ReturnsNegativeOne()
    {
        var games = new[] { new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build() };
        Assert.Equal(-1, MapGameToDbGameCleaned.GetStreak(games, 1));
    }

    [Fact]
    public void GetStreak_ConsecutiveWins_ReturnsPositiveCount()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
        };
        Assert.Equal(3, MapGameToDbGameCleaned.GetStreak(games, 1));
    }

    [Fact]
    public void GetStreak_ConsecutiveLosses_ReturnsNegativeCount()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(),
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(),
        };
        Assert.Equal(-4, MapGameToDbGameCleaned.GetStreak(games, 1));
    }

    [Fact]
    public void GetStreak_BrokenByLoss_CountsOnlyConsecutive()
    {
        // Most recent first (DateSortedTeamGames orders descending)
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(), // most recent: W
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(), // W
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(), // W
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.AWAY).Build(), // L — breaks streak
            new GameBuilder().WithTeams(1, 2).WithWinner(Winner.HOME).Build(),
        };
        Assert.Equal(3, MapGameToDbGameCleaned.GetStreak(games, 1));
    }

    #endregion

    #region GetShootingPct

    [Fact]
    public void GetShootingPct_EmptyList_ReturnsZero()
    {
        Assert.Equal(0, MapGameToDbGameCleaned.GetShootingPct([], 1));
    }

    [Fact]
    public void GetShootingPct_ZeroSOG_ReturnsZero()
    {
        var games = new[] { new GameBuilder().WithTeams(1, 2).WithSOG(0, 30).WithScore(0, 3).Build() };
        Assert.Equal(0, MapGameToDbGameCleaned.GetShootingPct(games, 1));
    }

    [Fact]
    public void GetShootingPct_CalculatesCorrectly()
    {
        var games = new[]
        {
            new GameBuilder().WithTeams(1, 2).WithSOG(30, 25).WithScore(3, 2).Build(),
            new GameBuilder().WithTeams(1, 2).WithSOG(20, 30).WithScore(3, 1).Build(),
        };
        // total goals = 6, total SOG = 50; 6/50 = 0.12
        Assert.Equal(0.12, MapGameToDbGameCleaned.GetShootingPct(games, 1));
    }

    #endregion

    #region GetStrengthOfSchedule

    [Fact]
    public void GetStrengthOfSchedule_EmptyList_ReturnsZero()
    {
        var seasonGames = new SeasonGames(new List<Game>());
        Assert.Equal(0, MapGameToDbGameCleaned.GetStrengthOfSchedule([], 1, seasonGames));
    }

    [Fact]
    public void GetStrengthOfSchedule_CalculatesAverageOpponentWinPct()
    {
        // Team 1 vs Team 2: Team 2 has a prior game where they won
        // Team 1 vs Team 3: Team 3 has a prior game where they lost
        var allGames = new List<Game>
        {
            // Team 2 beats team 3 (gives team 2 a 1.0 win ratio)
            new GameBuilder().WithId(1).WithTeams(2, 3).WithWinner(Winner.HOME)
                .WithDate(BaseDate).Build(),
            // Team 3 loses (gives team 3 a 0.0 win ratio)
            // (same game from team 3's perspective)

            // Team 1 plays team 2
            new GameBuilder().WithId(2).WithTeams(1, 2).WithWinner(Winner.HOME)
                .WithDate(BaseDate.AddDays(1)).Build(),
            // Team 1 plays team 3
            new GameBuilder().WithId(3).WithTeams(1, 3).WithWinner(Winner.HOME)
                .WithDate(BaseDate.AddDays(2)).Build(),
        };
        var seasonGames = new SeasonGames(allGames);

        var team1Games = new[]
        {
            allGames[1], // vs team 2
            allGames[2], // vs team 3
        };

        // Team 2 win ratio before game 2: 1.0 (won game 1)
        // Team 3 win ratio before game 3: 0.0 (lost game 1)
        // Average: (1.0 + 0.0) / 2 = 0.5
        Assert.Equal(0.5, MapGameToDbGameCleaned.GetStrengthOfSchedule(team1Games, 1, seasonGames));
    }

    #endregion

    #region Map integration tests

    [Fact]
    public void Map_WithNoPriorGames_AllStatsDefaultToZero()
    {
        var targetGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var seasonGames = new SeasonGames(new[] { targetGame });

        var result = MapGameToDbGameCleaned.Map(targetGame, seasonGames);

        Assert.Equal(1, result.GameId);
        Assert.Equal(0, result.HomeWinRatio);
        Assert.Equal(0, result.AwayWinRatio);
        Assert.Equal(0, result.HomeGoalsAvg);
        Assert.Equal(0, result.HomeStreak);
        Assert.Equal(0, result.AwayStreak);
        Assert.Equal(Game.DEFAULT_HOURS, result.HomeHoursSinceLastGame);
        Assert.Equal(Game.DEFAULT_HOURS, result.AwayHoursSinceLastGame);
        Assert.Equal(0, result.HomeRosterOffenseValue);
        Assert.Equal(0, result.HomeIsBackToBack);
        Assert.Equal(0, result.AwayIsBackToBack);
    }

    [Fact]
    public void Map_BackToBack_24Hours_IsBackToBack()
    {
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddHours(24)).Build();
        var seasonGames = new SeasonGames(new[] { priorGame, targetGame });

        var result = MapGameToDbGameCleaned.Map(targetGame, seasonGames);

        Assert.Equal(1.0, result.HomeIsBackToBack);
        Assert.Equal(1.0, result.AwayIsBackToBack);
    }

    [Fact]
    public void Map_BackToBack_29Hours_NotBackToBack()
    {
        var priorGame = new GameBuilder()
            .WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var targetGame = new GameBuilder()
            .WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddHours(29)).Build();
        var seasonGames = new SeasonGames(new[] { priorGame, targetGame });

        var result = MapGameToDbGameCleaned.Map(targetGame, seasonGames);

        Assert.Equal(0.0, result.HomeIsBackToBack);
        Assert.Equal(0.0, result.AwayIsBackToBack);
    }

    [Fact]
    public void Map_RestAdvantage_CalculatedCorrectly()
    {
        // Home team played 48h ago, away team played 24h ago
        var homeLastGame = new GameBuilder()
            .WithId(1).WithTeams(1, 3).WithDate(BaseDate).Build();
        var awayLastGame = new GameBuilder()
            .WithId(2).WithTeams(2, 3).WithDate(BaseDate.AddHours(24)).Build();
        var targetGame = new GameBuilder()
            .WithId(3).WithTeams(1, 2).WithDate(BaseDate.AddHours(48)).Build();
        var seasonGames = new SeasonGames(new[] { homeLastGame, awayLastGame, targetGame });

        var result = MapGameToDbGameCleaned.Map(targetGame, seasonGames);

        Assert.Equal(48.0, result.HomeHoursSinceLastGame);
        Assert.Equal(24.0, result.AwayHoursSinceLastGame);
        Assert.Equal(24.0, result.RestAdvantage);
    }

    [Fact]
    public void Map_WithMultiplePriorGames_CalculatesAverages()
    {
        var games = new List<Game>();
        // 6 prior games for team 1 (home) vs team 2
        for (int i = 0; i < 6; i++)
        {
            games.Add(new GameBuilder()
                .WithId(i + 1)
                .WithTeams(1, 2)
                .WithDate(BaseDate.AddDays(i))
                .WithScore(3 + i % 2, 2) // alternating 3-2 and 4-2
                .WithSOG(30, 25)
                .WithHits(20, 15)
                .Build());
        }

        var targetGame = new GameBuilder()
            .WithId(100).WithTeams(1, 2).WithDate(BaseDate.AddDays(10)).Build();
        games.Add(targetGame);

        var seasonGames = new SeasonGames(games);
        var result = MapGameToDbGameCleaned.Map(targetGame, seasonGames);

        Assert.Equal(100, result.GameId);
        // All 6 games are wins for team 1
        Assert.Equal(1.0, result.HomeWinRatio);
        Assert.Equal(0.0, result.AwayWinRatio);
        // Goals avg: (3+4+3+4+3+4)/6 = 3.5
        Assert.Equal(3.5, result.HomeGoalsAvg);
        // Recent (last 5): games 2-6 → (4+3+4+3+4)/5 = 3.6
        Assert.Equal(3.6, result.HomeRecentGoalsAvg, 5);
    }

    [Fact]
    public void Map_HeadToHeadWinRatio_OnlyCountsMatchups()
    {
        var games = new List<Game>
        {
            // Team 1 vs Team 2 — team 1 wins
            new GameBuilder().WithId(1).WithTeams(1, 2).WithWinner(Winner.HOME)
                .WithDate(BaseDate).Build(),
            // Team 1 vs Team 2 — team 2 wins
            new GameBuilder().WithId(2).WithTeams(1, 2).WithWinner(Winner.AWAY)
                .WithDate(BaseDate.AddDays(1)).Build(),
            // Team 1 vs Team 3 — team 1 wins (should not count)
            new GameBuilder().WithId(3).WithTeams(1, 3).WithWinner(Winner.HOME)
                .WithDate(BaseDate.AddDays(2)).Build(),
        };

        var targetGame = new GameBuilder()
            .WithId(10).WithTeams(1, 2).WithDate(BaseDate.AddDays(5)).Build();
        games.Add(targetGame);

        var seasonGames = new SeasonGames(games);
        var result = MapGameToDbGameCleaned.Map(targetGame, seasonGames);

        // Head to head: team 1 won 1 of 2 games vs team 2
        Assert.Equal(0.5, result.HeadToHeadWinRatio);
    }

    [Fact]
    public void Map_NullRosterScorer_RosterValuesAreZero()
    {
        var priorGame = new GameBuilder().WithId(1).WithTeams(1, 2).WithDate(BaseDate).Build();
        var targetGame = new GameBuilder().WithId(2).WithTeams(1, 2).WithDate(BaseDate.AddDays(1)).Build();
        var seasonGames = new SeasonGames(new[] { priorGame, targetGame });

        var result = MapGameToDbGameCleaned.Map(targetGame, seasonGames, rosterScorer: null);

        Assert.Equal(0, result.HomeRosterOffenseValue);
        Assert.Equal(0, result.HomeRosterDefenseValue);
        Assert.Equal(0, result.HomeRosterGoalieValue);
        Assert.Equal(0, result.AwayRosterOffenseValue);
        Assert.Equal(0, result.AwayRosterDefenseValue);
        Assert.Equal(0, result.AwayRosterGoalieValue);
    }

    #endregion
}
