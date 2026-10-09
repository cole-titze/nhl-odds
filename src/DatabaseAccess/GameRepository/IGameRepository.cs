using Entities.Models;

namespace DatabaseAccess.GameRepository;

public interface IGameRepository
{
    Task AddUpdateSeasonGameCount(int seasonStartYear, int seasonGameCount);
    Task AddUpdateGame(Game game);
    Task AddUpdateGameOfficials(Game game);
    Task AddUpdateGameCoaches(Game game);
    Task Commit();
    void ClearTracking();
    Task<Game?> GetGame(int gameId);
    Task<Game?> GetGameSummary(int gameId);
    Task<bool> IsGamePlayed(int gameId);
    Task<bool> IsUnplayedFutureGame(int gameId);
    Task<int> GetSavedGameCountForSeason(int seasonStartYear);
    Task<int?> GetGameCountForSeason(int seasonStartYear);
    Task<bool> HasPlayoffGamesForSeason(int seasonStartYear);
    Task<bool> HasPastUnplayedGamesForSeason(int seasonStartYear);
    Task<IList<int>> GetPlayedGameIdsSince(DateTime sinceUtc);
    Task DeleteGame(int gameId);
}