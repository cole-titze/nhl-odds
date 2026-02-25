using Entities.Models;

namespace DatabaseAccess.GameRepository;

public interface IGameRepository
{
    Task AddUpdateSeasonGameCount(int seasonStartYear, int seasonGameCount);
    Task AddUpdateGame(Game game);
    Task AddUpdateTvBroadcasters(Game game);
    Task AddUpdateGameTvBroadcasters(Game game);
    Task AddUpdateGameOfficials(Game game);
    Task AddUpdateGameCoaches(Game game);
    Task AddUpdateGameEvents(Game game);
    Task Commit();
    Task<Game?> GetGame(int gameId);
    Task<bool> IsGamePlayed(int gameId);
    Task<int> GetSavedGameCountForSeason(int seasonStartYear);
    Task<int?> GetGameCountForSeason(int seasonStartYear);
}

