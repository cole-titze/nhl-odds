using Entities.DbModels;
using Entities.Models;

namespace DatabaseAccess.GameEventRepository;

public interface IGameEventRepository
{
    Task AddUpdateGameEvents(Game game);
    Task<IEnumerable<IDbGameEvent>> GetAllDbGameEvents(int gameId);
}