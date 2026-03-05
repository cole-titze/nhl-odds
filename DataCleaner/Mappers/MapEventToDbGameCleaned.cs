using Entities.DbModels;
using Entities.Models;

namespace DataCleaner.Mappers;

public static class MapEventToDbGameCleaned
{
    public static void Apply(DbGameCleaned cleanedGame, EventAggregator? eventAggregator, Game game)
    {
        if (eventAggregator == null)
            return;

        var homeValues = eventAggregator.GetTeamEventValues(game.Id, game.HomeTeamId);
        var awayValues = eventAggregator.GetTeamEventValues(game.Id, game.AwayTeamId);

        cleanedGame.HomePpEfficiency = homeValues?.PpEfficiency ?? 0;
        cleanedGame.AwayPpEfficiency = awayValues?.PpEfficiency ?? 0;
        cleanedGame.HomeRecentPpEfficiency = homeValues?.RecentPpEfficiency ?? 0;
        cleanedGame.AwayRecentPpEfficiency = awayValues?.RecentPpEfficiency ?? 0;
        cleanedGame.HomePkEfficiency = homeValues?.PkEfficiency ?? 0;
        cleanedGame.AwayPkEfficiency = awayValues?.PkEfficiency ?? 0;
        cleanedGame.HomeRecentPkEfficiency = homeValues?.RecentPkEfficiency ?? 0;
        cleanedGame.AwayRecentPkEfficiency = awayValues?.RecentPkEfficiency ?? 0;
        cleanedGame.HomeGoalsPerGamePeriod1 = homeValues?.GoalsPerGamePeriod1 ?? 0;
        cleanedGame.AwayGoalsPerGamePeriod1 = awayValues?.GoalsPerGamePeriod1 ?? 0;
        cleanedGame.HomeGoalsPerGamePeriod2 = homeValues?.GoalsPerGamePeriod2 ?? 0;
        cleanedGame.AwayGoalsPerGamePeriod2 = awayValues?.GoalsPerGamePeriod2 ?? 0;
        cleanedGame.HomeGoalsPerGamePeriod3 = homeValues?.GoalsPerGamePeriod3 ?? 0;
        cleanedGame.AwayGoalsPerGamePeriod3 = awayValues?.GoalsPerGamePeriod3 ?? 0;
        cleanedGame.HomeRecentGoalsPerGamePeriod1 = homeValues?.RecentGoalsPerGamePeriod1 ?? 0;
        cleanedGame.AwayRecentGoalsPerGamePeriod1 = awayValues?.RecentGoalsPerGamePeriod1 ?? 0;
        cleanedGame.HomeRecentGoalsPerGamePeriod2 = homeValues?.RecentGoalsPerGamePeriod2 ?? 0;
        cleanedGame.AwayRecentGoalsPerGamePeriod2 = awayValues?.RecentGoalsPerGamePeriod2 ?? 0;
        cleanedGame.HomeRecentGoalsPerGamePeriod3 = homeValues?.RecentGoalsPerGamePeriod3 ?? 0;
        cleanedGame.AwayRecentGoalsPerGamePeriod3 = awayValues?.RecentGoalsPerGamePeriod3 ?? 0;
        cleanedGame.HomeOffensiveZoneFaceoffWinPct = homeValues?.OffensiveZoneFaceoffWinPct ?? 0;
        cleanedGame.AwayOffensiveZoneFaceoffWinPct = awayValues?.OffensiveZoneFaceoffWinPct ?? 0;
        cleanedGame.HomeRecentOffensiveZoneFaceoffWinPct = homeValues?.RecentOffensiveZoneFaceoffWinPct ?? 0;
        cleanedGame.AwayRecentOffensiveZoneFaceoffWinPct = awayValues?.RecentOffensiveZoneFaceoffWinPct ?? 0;
        cleanedGame.HomePenaltyDifferentialAvg = homeValues?.PenaltyDifferentialAvg ?? 0;
        cleanedGame.AwayPenaltyDifferentialAvg = awayValues?.PenaltyDifferentialAvg ?? 0;
        cleanedGame.HomeRecentPenaltyDifferentialAvg = homeValues?.RecentPenaltyDifferentialAvg ?? 0;
        cleanedGame.AwayRecentPenaltyDifferentialAvg = awayValues?.RecentPenaltyDifferentialAvg ?? 0;
    }
}
