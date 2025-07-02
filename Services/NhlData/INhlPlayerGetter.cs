using Entities.Models;

namespace Services.NhlData;

public interface INhlPlayerGetter
{
    Task<GameRosterStats?> BuildGameRosterStats(Game game);
    Task<Player?> GetPlayer(int playerId, int currentGameTeamId);
}

