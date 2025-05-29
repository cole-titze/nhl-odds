using Entities.DbModels;

namespace Services.NhlData
{
    public interface INhlGameGetter
	{
        Task<DbGameRaw> GetGame(int gameId);
    }
}

