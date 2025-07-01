using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapDbGameToGame
{
    public static Game Map(DbGameRaw game)
    {
        var extendedInfo = new GameExtendedInfo()
        {
            GameSummary = game.GameSummary,
            EventSummary = game.EventSummary,
            PlayByPlaySummary = game.PlayByPlaySummary,
            FaceoffSummary = game.FaceoffSummary,
            FaceoffComparisonSummary = game.FaceoffComparisonSummary,
            RosterSummary = game.RosterSummary,
            ShotSummary = game.ShotSummary,
            ShiftChartSummary = game.ShiftChartSummary,
            ToiAwaySummary = game.ToiAwaySummary,
            ToiHomeSummary = game.ToiHomeSummary,
            ThreeMinuteRecapVideoId = game.ThreeMinuteRecapVideoId,
            CondensedGameVideoId = game.CondensedGameVideoId,
            VenueName = game.VenueName,
            VenueLocation = game.VenueLocation,
        };
        return new Game()
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
            ExtendedInfo = extendedInfo,
        };
    }
    public static IEnumerable<Game> Map(IEnumerable<DbGameRaw> games)
    {
        var gameList = new List<Game>();
        foreach (var game in games)
        {
            gameList.Add(Map(game));
        }

        return gameList;
    }
}

