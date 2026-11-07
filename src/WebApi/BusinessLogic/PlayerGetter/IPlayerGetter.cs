using Entities.ViewModels;

namespace WebApi.BusinessLogic.PlayerGetter;

public interface IPlayerGetter
{
    /// <summary>null for an unknown player</summary>
    Task<PlayerVM?> GetPlayer(int playerId, int seasonStartYear);
    Task<List<RosterPlayerVM>> GetTeamRoster(int teamId, int seasonStartYear);
    /// <summary>null for an unknown game</summary>
    Task<GamePlayersVM?> GetGamePlayers(int gameId);
    Task<List<ActivePlayerVM>> GetActivePlayers(int seasonStartYear);
}
