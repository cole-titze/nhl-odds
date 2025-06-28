namespace Entities.Models.GamePlayEvents;

public class GameEvents
{
    public IEnumerable<IGameEvent> Events { get; set; }
    public GameEvents(IEnumerable<IGameEvent> events)
    {
        this.Events = events;
    }
}