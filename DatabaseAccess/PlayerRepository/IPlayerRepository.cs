using Entities.Models;

namespace DatabaseAccess.PlayerRepository;

public interface IPlayerRepository
{
    Task AddUpdateGameRosterStats(IEnumerable<Game> games);
    Task AddUpdatePlayerDraftDetails(IEnumerable<Player> players);
    Task AddUpdatePlayers(IEnumerable<Player> players);
    Task<int> GetPlayerStatsCountBySeason(int seasonStartYear);
}

