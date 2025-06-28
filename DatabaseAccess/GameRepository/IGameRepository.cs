using Entities.Models;

namespace DatabaseAccess.GameRepository;

public interface IGameRepository
{
    Task AddSeasonGameCounts(IDictionary<int, int> seasonGameCountCache);
    Task AddUpdateGames(IEnumerable<Game> seasonGames);
    Task AddUpdateTvBroadcasters(IEnumerable<Game> games);
    Task AddUpdateGameTvBroadcasters(IEnumerable<Game> games);
    Task AddUpdateGameOfficials(IEnumerable<Game> games);
    Task AddUpdateGameEvents(IEnumerable<Game> games);
    Task Commit();
    Task<Game?> GetGame(int gameId);
    Task<int> GetGameCountInSeason(int seasonStartYear);
}

