using Entities.Models;

namespace DatabaseAccess.PlayerRepository;

public interface IPlayerRepository
{
    Task AddUpdateGameRosterStats(Game game);
    Task AddUpdatePlayerDraftDetails(IEnumerable<Player> players);
    Task AddUpdatePlayers(IEnumerable<Player> players);
    Task<int> GetPlayerStatsCountBySeason(int seasonStartYear);
    Task<Player?> GetPlayer(int playerId);
    Task<List<int>> GetGameIdsWithoutGoalieShortHandedStats(int seasonStartYear);
    Task<int> UpdateGoalieShortHandedStats(int gameId, IEnumerable<GameGoalieStats> goalieStats);
    Task Commit();
}