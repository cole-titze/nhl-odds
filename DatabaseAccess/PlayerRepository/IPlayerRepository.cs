using Entities.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DatabaseAccess.PlayerRepository;

public interface IPlayerRepository
{
    Task AddUpdateGameRosterStats(Game game);
    Task AddUpdatePlayerDraftDetails(IEnumerable<Player> players);
    Task AddUpdatePlayers(IEnumerable<Player> players);
    Task<int> GetPlayerStatsCountBySeason(int seasonStartYear);
    Task<Player?> GetPlayer(int playerId);
    Task Commit();
}

