using Entities.DbModels;
using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbGameEventsToGameEvents
{
    public static GameEvents Map(IEnumerable<IDbGameEvent> dbEvents)
    {
        var events = new List<IGameEvent>();
        foreach (var db in dbEvents)
        {
            IGameEvent? mapped = db switch
            {
                DbBlockedShot e => MapDbBlockedShotEventToBlockedShotEvent.Map(e),
                DbDelayedPenalty e => MapDbDelayedPenaltyEventToDelayedPenaltyEvent.Map(e),
                DbFaceoff e => MapDbFaceoffEventToFaceoffEvent.Map(e),
                DbGameEnd e => MapDbGameEndEventToGameEndEvent.Map(e),
                DbGiveaway e => MapDbGiveawayEventToGiveawayEvent.Map(e),
                DbGoal e => MapDbGoalEventToGoalEvent.Map(e),
                DbHit e => MapDbHitEventToHitEvent.Map(e),
                DbMissedShot e => MapDbMissedShotEventToMissedShotEvent.Map(e),
                DbPenalty e => MapDbPenaltyEventToPenaltyEvent.Map(e),
                DbPeriodStart e => MapDbPeriodStartEventToPeriodStartEvent.Map(e),
                DbPeriodEnd e => MapDbPeriodEndEventToPeriodEndEvent.Map(e),
                DbShot e => MapDbShotEventToShotEvent.Map(e),
                DbShootoutComplete e => MapDbShootoutCompleteEventToShootoutCompleteEvent.Map(e),
                DbStoppage e => MapDbStoppageEventToStoppageEvent.Map(e),
                DbTakeaway e => MapDbTakeawayEventToTakeawayEvent.Map(e),
                DbFailedShotAttempt e => MapDbFailedShotAttemptEventToFailedShotAttemptEvent.Map(e),
                _ => null
            };
            if (mapped != null) events.Add(mapped);
        }
        return new GameEvents(events);
    }
}