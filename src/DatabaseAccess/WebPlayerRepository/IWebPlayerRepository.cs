using Entities.Models.Web;

namespace DatabaseAccess.WebPlayerRepository;

public interface IWebPlayerRepository
{
    /// <summary>The player's bio and the team, position and number of his latest game; null for an unknown player.</summary>
    Task<PlayerProfile?> GetPlayer(int playerId);
    /// <summary>His box score lines in the season's games, newest first.</summary>
    Task<List<PlayerGameLine>> GetPlayerGames(int playerId, int seasonStartYear);
    /// <summary>Players whose latest game in the season was for the team.</summary>
    Task<List<RosterPlayer>> GetTeamRoster(int teamId, int seasonStartYear);
    /// <summary>The game's box score once played, else both teams' rosters; null for an unknown game.</summary>
    Task<GamePlayers?> GetGamePlayers(int gameId);
    /// <summary>Every player with a game in the season, and when his last one started.</summary>
    Task<List<ActivePlayer>> GetActivePlayers(int seasonStartYear);
    /// <summary>Every player with a page and at least one game, each with their last game (for /sitemap.xml)</summary>
    Task<List<ActivePlayer>> GetPlayersLastGames();
    /// <summary>Every player's id and name, for finding players by name.</summary>
    Task<List<PlayerName>> GetPlayerNames();
    /// <summary>The team and position of each player's latest game; players without games are left out.</summary>
    Task<List<PlayerLatestTeam>> GetLatestTeams(IReadOnlyCollection<int> playerIds);
    /// <summary>Every player's regular-season totals, with the team and position of their latest game (#182).</summary>
    Task<List<PlayerSeasonTotals>> GetSeasonTotals(int seasonStartYear);
}
