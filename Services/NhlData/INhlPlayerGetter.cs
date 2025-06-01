using Entities.Models;

namespace Services.NhlData
{
    public interface INhlPlayerGetter
	{
        Task<IEnumerable<IGamePlayerStats>?> GetPlayerGameStats(Game game);
        Task<Player> GetPlayer(int playerId);
    }
}

