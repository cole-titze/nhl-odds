using System.ComponentModel;
using Entities.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using WebApi.BusinessLogic.PlayerGetter;
using WebApi.Caching;
using WebApi.Mappers;

namespace WebApi.McpTools;

[McpServerToolType]
public class PlayerMcpTools(IPlayerGetter playerGetter, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
{
    private const int MaxMatches = 10;

    // Mirrors getCurrentSeason in frontend/src/utils/season.ts: a new season starts on September 20
    private static int CurrentSeason(DateTime date) =>
        date >= new DateTime(date.Year, 9, 20) ? date.Year : date.Year - 1;

    [McpServerTool, Description("Find NHL players by name to get their player IDs (needed by GetPlayer). Matches first, last or full name, ignoring case and accents; the start of a last name is enough (e.g. \"mcdav\"). Returns up to 10, players with a game this season first, each with position and the team of his latest game.")]
    public async Task<List<PlayerSearchResultVM>> FindPlayers(
        [Description("All or part of the player's name, e.g. \"McDavid\", \"connor mcd\" or \"stutzle\"")] string name)
    {
        var season = CurrentSeason(DateTime.UtcNow);
        var folded = PlayerNameMatcher.Fold(name ?? string.Empty);
        if (folded.Length < 2 || folded.Length > 60)
            throw new McpException("Give between 2 and 60 letters of the player's name.");
        return await cache.GetOrSetAsync(ApiCache.FindPlayersKey(folded, season),
            () => playerGetter.FindPlayers(folded, season, MaxMatches), r => 1 + r.Count / 10, ApiCache.DateRangeLifetime);
    }

    [McpServerTool, Description("Get one NHL player's season by player ID (from FindPlayers, or the number in a /player/{id} page URL): bio, position, the team of his latest game, season totals and per-game averages, and every game that season (newest first) with result and his stats: goals, assists, shots, +/- and time on ice for skaters; shots against, saves and goals against for goalies.")]
    public async Task<PlayerStatsVM> GetPlayer(
        [Description("The player ID")] int playerId,
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
    {
        // Shares the Player page's cache entry
        var player = await cache.GetOrSetAsync(ApiCache.PlayerKey(playerId, seasonStartYear),
            () => playerGetter.GetPlayer(playerId, seasonStartYear), ApiCache.SizeOf, ApiCache.DateRangeLifetime);
        if (player is null)
            throw new McpException($"No player with ID {playerId}. Use FindPlayers to look up a player's ID by name.");
        return PlayerVmToPlayerStatsVmMapper.Map(player);
    }

    [McpServerTool, Description("Get an NHL team's roster for a season: the players whose latest game that season was for the team (forwards, then defense, then goalies), with player ID, position, sweater number and games played.")]
    public async Task<List<RosterPlayerVM>> GetTeamRoster(
        [Description("The team ID")] int teamId,
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
        => await cache.GetOrSetAsync(ApiCache.TeamRosterKey(teamId, seasonStartYear),
            () => playerGetter.GetTeamRoster(teamId, seasonStartYear), r => r.Count, ApiCache.DateRangeLifetime);
}
