using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapDbGameToGame
{
    public static Game Map(DbGameRaw game)
    {
        var extendedInfo = new GameExtendedInfo()
        {
            GameSummary = game.GameSummary ?? string.Empty,
            EventSummary = game.EventSummary ?? string.Empty,
            PlayByPlaySummary = game.PlayByPlaySummary ?? string.Empty,
            FaceoffSummary = game.FaceoffSummary ?? string.Empty,
            FaceoffComparisonSummary = game.FaceoffComparisonSummary ?? string.Empty,
            RosterSummary = game.RosterSummary ?? string.Empty,
            ShotSummary = game.ShotSummary ?? string.Empty,
            ShiftChartSummary = game.ShiftChartSummary ?? string.Empty,
            ToiAwaySummary = game.ToiAwaySummary ?? string.Empty,
            ToiHomeSummary = game.ToiHomeSummary ?? string.Empty,
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
}

