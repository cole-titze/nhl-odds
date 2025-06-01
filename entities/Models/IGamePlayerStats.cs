namespace Entities.Models
{
    public interface IGamePlayerStats
    {
        int playerId { get; set; }
        int gameId { get; set; }
    }
}