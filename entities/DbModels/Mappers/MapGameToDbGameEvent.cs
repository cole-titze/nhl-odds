using Entities.DbModels.Mappers.GameEventMappers;
using Entities.Models;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers
{
    public static class MapGameToDbGameEvent
    {
        public static IEnumerable<IDbGameEvent> MapList(IEnumerable<Game> seasonGames)
		{
            var dbGameEvents = new List<IDbGameEvent>();
            foreach (var game in seasonGames)
            {
                if (game.gameEvents == null)
                    throw new ArgumentNullException(nameof(game.gameEvents), "gameEvents cannot be null");

                dbGameEvents.AddRange(MapShotEventToDbShotEvent.MapList(game.gameEvents.events.OfType<Shot>(), game.id));
                dbGameEvents.AddRange(MapBlockedShotEventToDbBlockedShotEvent.MapList(game.gameEvents.events.OfType<BlockedShot>(), game.id));
                dbGameEvents.AddRange(MapDelayedPenaltyEventToDbDelayedPenaltyEvent.MapList(game.gameEvents.events.OfType<DelayedPenalty>(), game.id));
                dbGameEvents.AddRange(MapFaceoffEventToDbFaceoffEvent.MapList(game.gameEvents.events.OfType<Faceoff>(), game.id));
                dbGameEvents.AddRange(MapGameEndEventToDbGameEndEvent.MapList(game.gameEvents.events.OfType<GameEnd>(), game.id));
                dbGameEvents.AddRange(MapGiveawayEventToDbGiveawayEvent.MapList(game.gameEvents.events.OfType<Giveaway>(), game.id));
                dbGameEvents.AddRange(MapGoalEventToDbGoalEvent.MapList(game.gameEvents.events.OfType<Goal>(), game.id));
                dbGameEvents.AddRange(MapHitEventToDbHitEvent.MapList(game.gameEvents.events.OfType<Hit>(), game.id));
                dbGameEvents.AddRange(MapMissedShotEventToDbMissedShotEvent.MapList(game.gameEvents.events.OfType<MissedShot>(), game.id));
                dbGameEvents.AddRange(MapPenaltyEventToDbPenaltyEvent.MapList(game.gameEvents.events.OfType<Penalty>(), game.id));
                dbGameEvents.AddRange(MapPeriodStartEventToDbPeriodStartEvent.MapList(game.gameEvents.events.OfType<PeriodStart>(), game.id));
                dbGameEvents.AddRange(MapStoppageEventToDbStoppageEvent.MapList(game.gameEvents.events.OfType<Stoppage>(), game.id));
                dbGameEvents.AddRange(MapTakeawayEventToDbTakeawayEvent.MapList(game.gameEvents.events.OfType<Takeaway>(), game.id));
                dbGameEvents.AddRange(MapShootoutCompleteEventToDbShootoutCompleteEvent.MapList(game.gameEvents.events.OfType<ShootoutComplete>(), game.id));
            }

            return dbGameEvents;
		}
    }
}

