using Entities.ViewModels;

namespace WebApi.BusinessLogic.PlayerGetter;

public interface IPlayerGetter
{
    /// <summary>null for an unknown player</summary>
    Task<PlayerVM?> GetPlayer(int playerId, int seasonStartYear);
    Task<List<RosterPlayerVM>> GetTeamRoster(int teamId, int seasonStartYear);
    /// <summary>null for an unknown game</summary>
    Task<GamePlayersVM?> GetGamePlayers(int gameId);
    /// <summary>
    /// Every player with a page and a game, each with their last game's start, for /sitemap.xml. A player with no
    /// game, or (bad data) only games after <paramref name="nowUtc"/>, is left out rather than given a made-up date.
    /// </summary>
    Task<List<ActivePlayerVM>> GetSitemapPlayers(DateTime nowUtc);
    /// <summary>Players matching a name (see PlayerNameMatcher), those with a game in the season first.</summary>
    Task<List<PlayerSearchResultVM>> FindPlayers(string name, int seasonStartYear, int limit);
}
