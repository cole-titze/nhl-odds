using Entities.Models;

namespace DatabaseAccess.GameRepository;

public interface IGameRepository
{
    Task AddSeasonGameCounts(IDictionary<int, int> seasonGameCountCache);
    Task AddUpdateGame(Game game);
    Task AddUpdateTvBroadcasters(Game game);
    Task AddUpdateGameTvBroadcasters(Game game);
    Task AddUpdateGameOfficials(Game game);
    Task AddUpdateGameEvents(Game game);
    Task Commit();
    Task<Game?> GetGame(int gameId);
    Task<int> GetGameCountInSeason(int seasonStartYear);
}

