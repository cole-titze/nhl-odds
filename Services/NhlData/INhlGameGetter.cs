using Entities.Models;

namespace Services.NhlData
{
    public interface INhlGameGetter
    {
        Task<Game?> GetGame(int gameId);
    }
}

