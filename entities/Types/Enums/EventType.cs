namespace Entities.Types.Enums
{
    public enum EventType
    {
        BlockedShot = 1,
        DelayedPenalty = 2,
        Faceoff = 3,
        GameEnd = 4,
        GameEvents = 5,
        Giveaway = 6,
        Goal = 7,
        Hit = 8,
        MissedShot = 9,
        Penalty = 10,
        PeriodStart = 11,
        Shot = 12,
        Stoppage = 13,
        Takeaway = 14
    }
    public static class EventTypeParser {
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
                case "Penalty":
                    return new Penalty(responseGameEvent);
                case "Faceoff":
                    return new Faceoff(responseGameEvent);
                case "Blocked Shot":
                    return new BlockedShot(responseGameEvent);
                case "Missed Shot":
                    return new MissedShot(responseGameEvent);
                default:
                    return new UnknownEvent(responseGameEvent);
            }
        }
    }
}