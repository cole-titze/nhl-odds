namespace Entities.Types.Enums;

public enum EventType
{
    BlockedShot = 1,
    DelayedPenalty = 2,
    Faceoff = 3,
    GameEnd = 4,
    Giveaway = 5,
    Goal = 6,
    Hit = 7,
    MissedShot = 8,
    Penalty = 9,
    PeriodStart = 10,
    Shot = 11,
    Stoppage = 12,
    Takeaway = 13,
    PeriodEnd = 14,
    ShootoutComplete = 15
}
public static class EventTypeParser
{
    public static EventType Parse(string eventType)
    {
        switch (eventType)
        {
            case "period-start":
                return EventType.PeriodStart;
            case "faceoff":
                return EventType.Faceoff;
            case "shot-on-goal":
                return EventType.Shot;
            case "stoppage":
                return EventType.Stoppage;
            case "missed-shot":
                return EventType.MissedShot;
            case "goal":
                return EventType.Goal;
            case "hit":
                return EventType.Hit;
            case "giveaway":
                return EventType.Giveaway;
            case "takeaway":
                return EventType.Takeaway;
            case "blocked-shot":
                return EventType.BlockedShot;
            case "delayed-penalty":
                return EventType.DelayedPenalty;
            case "penalty":
                return EventType.Penalty;
            case "game-end":
                return EventType.GameEnd;
            case "period-end":
                return EventType.PeriodEnd;
            case "shootout-complete":
                return EventType.ShootoutComplete;
            default:
                throw new ArgumentException($"Unknown event type: {eventType}", nameof(eventType));
        }
    }
}