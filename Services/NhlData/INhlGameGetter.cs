using Entities.Models;
using Entities.Models.GamePlayEvents;

namespace Services.NhlData
{
    public interface INhlGameGetter
	{
        Task<Game?> GetGame(int gameId);
    }
}

