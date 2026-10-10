namespace Entities.Models.GamePlayEvents;

public class GameEvents
{
    public IEnumerable<IGameEvent> Events { get; set; }
    // Ids of every play in the response, including ones that couldn't be mapped
    public IReadOnlySet<int> SourceEventIds { get; set; } = new HashSet<int>();
    public GameEvents(IEnumerable<IGameEvent> events)
    {
        this.Events = events;
    }
}