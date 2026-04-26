using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapGameToDbGame
{
    public static DbGameRaw Map(Game game)
    {
        return new DbGameRaw()
        {
            Id = game.Id,
            HomeTeamId = game.HomeTeamId,
            AwayTeamId = game.AwayTeamId,
            SeasonStartYear = game.SeasonStartYear,
            GameDateUTC = game.GameDateUTC,
            HomeGoals = game.HomeGoals,
            AwayGoals = game.AwayGoals,
            Winner = game.Winner,
            EndPeriod = game.EndPeriod,
            HomeSOG = game.HomeSOG,
            AwaySOG = game.AwaySOG,
            HomePPG = game.HomePPG,
            AwayPPG = game.AwayPPG,
            HomePIM = game.HomePIM,
            AwayPIM = game.AwayPIM,
            HomeFaceOffWinPercent = game.HomeFaceOffWinPercent,
            AwayFaceOffWinPercent = game.AwayFaceOffWinPercent,
            HomeBlockedShots = game.HomeBlockedShots,
            AwayBlockedShots = game.AwayBlockedShots,
            HomeHits = game.HomeHits,
            AwayHits = game.AwayHits,
            HomeTakeaways = game.HomeTakeaways,
            AwayTakeaways = game.AwayTakeaways,
            HomeGiveaways = game.HomeGiveaways,
            AwayGiveaways = game.AwayGiveaways,
            HasBeenPlayed = game.HasBeenPlayed,
            GameSummary = game.ExtendedInfo?.GameSummary,
            EventSummary = game.ExtendedInfo?.EventSummary,
            PlayByPlaySummary = game.ExtendedInfo?.PlayByPlaySummary,
            FaceoffSummary = game.ExtendedInfo?.FaceoffSummary,
            FaceoffComparisonSummary = game.ExtendedInfo?.FaceoffComparisonSummary,
            RosterSummary = game.ExtendedInfo?.RosterSummary,
            ShotSummary = game.ExtendedInfo?.ShotSummary,
            ShiftChartSummary = game.ExtendedInfo?.ShiftChartSummary,
            ToiAwaySummary = game.ExtendedInfo?.ToiAwaySummary,
            ToiHomeSummary = game.ExtendedInfo?.ToiHomeSummary,
            ThreeMinuteRecapVideoId = game.ExtendedInfo?.ThreeMinuteRecapVideoId ?? 0,
            CondensedGameVideoId = game.ExtendedInfo?.CondensedGameVideoId ?? 0,
            VenueName = game.ExtendedInfo?.VenueName ?? string.Empty,
            VenueLocation = game.ExtendedInfo?.VenueLocation ?? string.Empty,
        };
    }
    public static IEnumerable<DbGameRaw> MapList(IEnumerable<Game> games)
    {
        var dbGames = new List<DbGameRaw>();
        foreach (var game in games)
        {
            dbGames.Add(Map(game));
        }
        return dbGames;
    }
}