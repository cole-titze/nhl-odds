using Entities.DbModels;

namespace Services.NhlData
{
    public interface INhlPlayerGetter
	{
        Task<IEnumerable<IDbGamePlayerStats>> GetPlayerGameStats(DbGameRaw game);
        Task<DbPlayer> GetPlayer(int playerId);
    }
}

