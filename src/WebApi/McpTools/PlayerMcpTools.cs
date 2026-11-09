using System.ComponentModel;
using Entities.Types;
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

    [McpServerTool, Description("Find NHL players by name to get their player IDs (needed by GetPlayer). Matches first, last or full name, ignoring case and accents; the start of a last name is enough (e.g. \"mcdav\"). Returns up to 10, players with a game this season first, each with position, current team (the NHL's roster report; empty when on no NHL roster) and the team of their latest game.")]
    public async Task<List<PlayerSearchResultVM>> FindPlayers(
        [Description("All or part of the player's name, e.g. \"McDavid\", \"connor mcd\" or \"stutzle\"")] string name)
    {
        var season = NhlSeason.Current(DateTime.UtcNow);
        var folded = PlayerNameMatcher.Fold(name ?? string.Empty);
        if (folded.Length < 2 || folded.Length > 60)
            throw new McpException("Give between 2 and 60 letters of the player's name.");
        return await cache.GetOrSetAsync(ApiCache.FindPlayersKey(folded, season),
            () => playerGetter.FindPlayers(folded, season, MaxMatches), r => 1 + r.Count / 10, ApiCache.DateRangeLifetime);
    }

    [McpServerTool, Description("Get one NHL player's season by player ID (from FindPlayers, or the number in a /player/{id} page URL): bio, position, current team from the NHL's roster report (with injured reserve; null when on no NHL roster, then LastPlayedFor names the team and season of their latest game), season totals and per-game averages, and every game that season (newest first) with result and their stats: goals, assists, shots, +/- and time on ice for skaters; shots against, saves and goals against for goalies.")]
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

    [McpServerTool, Description("Get an NHL team's roster for a season. The current season: the players the NHL's latest roster report lists for the team, injured reserve marked, games played for the team this season (0 if none yet). Past seasons: the players whose latest game that season was for the team. Listed forwards, then defense, then goalies, with player ID, position, sweater number and games played.")]
    public async Task<List<RosterPlayerVM>> GetTeamRoster(
        [Description("The team ID")] int teamId,
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
        => await cache.GetOrSetAsync(ApiCache.TeamRosterKey(teamId, seasonStartYear),
            () => playerGetter.GetTeamRoster(teamId, seasonStartYear), r => r.Count, ApiCache.DateRangeLifetime);
}
