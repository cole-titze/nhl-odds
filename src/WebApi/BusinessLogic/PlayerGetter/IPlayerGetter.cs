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
    /// <summary>Players matching a name (see PlayerNameMatcher), those with a game in the season first.</summary>
    Task<List<PlayerSearchResultVM>> FindPlayers(string name, int seasonStartYear, int limit);
}
