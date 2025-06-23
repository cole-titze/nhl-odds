namespace Entities.Models.GamePlayEvents
{
    public class GameEvents
    {
        public IEnumerable<IGameEvent> events { get; set; }
        public GameEvents(IEnumerable<IGameEvent> events)
        {
            this.events = events;
        }
    }
}