using Entities.Models.GamePlayEvents;
using Entities.ServiceModels.Mappers.GameEventMappers;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers;

public static class MapGameEventsResponseToGameEvents
{
    public static GameEvents Map(dynamic response)
    {
        var gameEvents = new List<IGameEvent>();
        foreach (var responseGameEvent in response.plays)
        {
            var gameEvent = GetGameEvent(responseGameEvent);
            if (gameEvent != null)
            {
                gameEvents.Add(gameEvent);
            }
        }

        return new GameEvents(gameEvents);
    }

    /// <summary>
    ///  Maps a single game event from the response to a GameEvent object.
    /// </summary>
    /// <param name="responseGameEvent">A game event from the nhl api</param>
    /// <returns>The Game Event</returns>
    private static IGameEvent? GetGameEvent(dynamic responseGameEvent)
    {
        var eventType = EventTypeParser.Parse((string)responseGameEvent.typeDescKey);

        // Some event types require details; skip if details is missing
        if (responseGameEvent.details == null)
        {
            switch (eventType)
            {
                case EventType.PeriodStart:
                case EventType.PeriodEnd:
                case EventType.GameEnd:
                case EventType.ShootoutComplete:
                    break; // These don't need details
                default:
                    return null; // Skip events that need details but don't have them
            }
        }

        switch (eventType)
        {
            case EventType.PeriodStart:
                return MapPeriodStartEvent.Map(responseGameEvent);
            case EventType.Faceoff:
                return MapFaceoffEvent.Map(responseGameEvent);
            case EventType.Shot:
                return MapShotEvent.Map(responseGameEvent);
            case EventType.Stoppage:
                return MapStoppageEvent.Map(responseGameEvent);
            case EventType.BlockedShot:
                return MapBlockedShotEvent.Map(responseGameEvent);
            case EventType.DelayedPenalty:
                return MapDelayedPenaltyEvent.Map(responseGameEvent);
            case EventType.GameEnd:
                return MapGameEndEvent.Map(responseGameEvent);
            case EventType.Giveaway:
                return MapGiveawayEvent.Map(responseGameEvent);
            case EventType.Goal:
                return MapGoalEvent.Map(responseGameEvent);
            case EventType.Hit:
                return MapHitEvent.Map(responseGameEvent);
            case EventType.MissedShot:
                return MapMissedShotEvent.Map(responseGameEvent);
            case EventType.FailedShotAttempt:
                return MapFailedShotAttemptEvent.Map(responseGameEvent);
            case EventType.Penalty:
                return MapPenaltyEvent.Map(responseGameEvent);
            case EventType.Takeaway:
                return MapTakeawayEvent.Map(responseGameEvent);
            case EventType.PeriodEnd:
                return MapPeriodEndEvent.Map(responseGameEvent);
            case EventType.ShootoutComplete:
                return MapShootoutCompleteEvent.Map(responseGameEvent);
            default:
                throw new ArgumentException(nameof(eventType), $"Unknown event type: {eventType}");
        }
    }
}