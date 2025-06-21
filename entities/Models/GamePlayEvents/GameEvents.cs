namespace Entities.Models.GamePlayEvents
{
    public class GameEvents
    {
        public IEnumerable<IGameEvent> events { get; set; } = new List<IGameEvent>();
    }
}