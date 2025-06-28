using Entities.DbModels.Mappers.GameEventMappers;
using Entities.Models;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers;

public static class MapGameToDbGameEvent
{
    public static IEnumerable<IDbGameEvent> MapList(IEnumerable<Game> seasonGames)
    {
        var dbGameEvents = new List<IDbGameEvent>();
        foreach (var game in seasonGames)
        {
            if (game.GameEvents == null)
                throw new ArgumentNullException(nameof(game.GameEvents), "gameEvents cannot be null");

            dbGameEvents.AddRange(MapShotEventToDbShotEvent.MapList(game.GameEvents.Events.OfType<Shot>(), game.Id));
            dbGameEvents.AddRange(MapBlockedShotEventToDbBlockedShotEvent.MapList(game.GameEvents.Events.OfType<BlockedShot>(), game.Id));
            dbGameEvents.AddRange(MapDelayedPenaltyEventToDbDelayedPenaltyEvent.MapList(game.GameEvents.Events.OfType<DelayedPenalty>(), game.Id));
            dbGameEvents.AddRange(MapFaceoffEventToDbFaceoffEvent.MapList(game.GameEvents.Events.OfType<Faceoff>(), game.Id));
            dbGameEvents.AddRange(MapGameEndEventToDbGameEndEvent.MapList(game.GameEvents.Events.OfType<GameEnd>(), game.Id));
            dbGameEvents.AddRange(MapGiveawayEventToDbGiveawayEvent.MapList(game.GameEvents.Events.OfType<Giveaway>(), game.Id));
            dbGameEvents.AddRange(MapGoalEventToDbGoalEvent.MapList(game.GameEvents.Events.OfType<Goal>(), game.Id));
            dbGameEvents.AddRange(MapHitEventToDbHitEvent.MapList(game.GameEvents.Events.OfType<Hit>(), game.Id));
            dbGameEvents.AddRange(MapMissedShotEventToDbMissedShotEvent.MapList(game.GameEvents.Events.OfType<MissedShot>(), game.Id));
            dbGameEvents.AddRange(MapPenaltyEventToDbPenaltyEvent.MapList(game.GameEvents.Events.OfType<Penalty>(), game.Id));
            dbGameEvents.AddRange(MapPeriodStartEventToDbPeriodStartEvent.MapList(game.GameEvents.Events.OfType<PeriodStart>(), game.Id));
            dbGameEvents.AddRange(MapStoppageEventToDbStoppageEvent.MapList(game.GameEvents.Events.OfType<Stoppage>(), game.Id));
            dbGameEvents.AddRange(MapTakeawayEventToDbTakeawayEvent.MapList(game.GameEvents.Events.OfType<Takeaway>(), game.Id));
            dbGameEvents.AddRange(MapShootoutCompleteEventToDbShootoutCompleteEvent.MapList(game.GameEvents.Events.OfType<ShootoutComplete>(), game.Id));
        }

        return dbGameEvents;
    }
}

