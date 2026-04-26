using Entities.Models;

namespace DatabaseAccess.GameSeasonRepository;

public interface IGameSeasonRepository
{
    Task<IEnumerable<Game>> GetSeasonGames(int seasonStartYear);
}