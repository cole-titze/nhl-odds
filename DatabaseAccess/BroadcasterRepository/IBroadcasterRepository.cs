using Entities.Models;

namespace DatabaseAccess.BroadcasterRepository;

public interface IBroadcasterRepository
{
    Task AddUpdateTvBroadcasters(Game game);
    Task AddUpdateGameTvBroadcasters(Game game);
}