using Entities.DbModels;
using Entities.Mappers.GameMappers;
using Entities.Mappers.PlayerMappers;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.PlayerRepository;

public class PlayerRepository : IPlayerRepository
{
    private readonly NhlDbContext _dbContext;
    public PlayerRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    /// <summary>
    /// Add Players to database if they don't exist, otherwise update them
    /// </summary>
    /// <param name="playersWithValues">List of players to store</param>
    /// <returns>None</returns>
    public async Task AddUpdatePlayers(IEnumerable<Player> players)
    {
        var dbPlayers = MapPlayerToDbPlayer.Map(players);

        var addList = new List<DbPlayer>();
        var updateList = new List<DbPlayer>();
        foreach (var player in dbPlayers)
        {
            var dbPlayer = await GetDbPlayer(player.Id);
            if (dbPlayer == null)
            {
                addList.Add(player);
            }
            else if (!dbPlayer.IsEquivalentTo(player))
            {
                dbPlayer.Clone(player);
                updateList.Add(dbPlayer);
            }
        }

        await _dbContext.Player.AddRangeAsync(addList);
        _dbContext.Player.UpdateRange(updateList);
    }
    /// <summary>
    /// Add Players stats per game to database if they don't exist, otherwise update them
    /// </summary>
    /// <param name="game">Game to store player stats for</param>
    /// <returns>None</returns>
    public async Task AddUpdateGameRosterStats(Game game)
    {
        var dbGamePlayerStats = MapGameToDbGamePlayerStats.Map(game).ToList();

        var addList = new List<IDbGamePlayerStats>();
        var updateList = new List<IDbGamePlayerStats>();
        foreach (var playerStats in dbGamePlayerStats)
        {
            var dbPlayerStats = await GetDbGamePlayerStats(playerStats);
            if (dbPlayerStats == null)
            {
                addList.Add(playerStats);
            }
            else if (!dbPlayerStats.IsEquivalentTo(playerStats))
            {
                dbPlayerStats.Clone(playerStats);
                updateList.Add(dbPlayerStats);
            }
        }

        await _dbContext.GameSkaterStats.AddRangeAsync(addList.OfType<DbGameSkaterStats>());
        _dbContext.GameSkaterStats.UpdateRange(updateList.OfType<DbGameSkaterStats>());
        await _dbContext.GameGoalieStats.AddRangeAsync(addList.OfType<DbGameGoalieStats>());
        _dbContext.GameGoalieStats.UpdateRange(updateList.OfType<DbGameGoalieStats>());

        // Only called for played games, so the box score is authoritative: drop rows for players
        // who didn't play (e.g. left over from a pre-game current-roster snapshot)
        if (dbGamePlayerStats.Count == 0)
            return;
        var playedIds = dbGamePlayerStats.Select(p => p.PlayerId).ToHashSet();
        _dbContext.GameSkaterStats.RemoveRange(await _dbContext.GameSkaterStats
            .Where(x => x.GameId == game.Id && !playedIds.Contains(x.PlayerId)).ToListAsync());
        _dbContext.GameGoalieStats.RemoveRange(await _dbContext.GameGoalieStats
            .Where(x => x.GameId == game.Id && !playedIds.Contains(x.PlayerId)).ToListAsync());
    }
    /// <summary>
    /// Gets the season's games whose goalie rows have no shorthanded shots or goals at all. Games saved before the
    /// goalie mapper read the API's "shorthanded…" fields have zeros there; a few games are genuinely zero.
    /// </summary>
    /// <param name="seasonStartYear">Season to check</param>
    /// <returns>Game ids, ascending</returns>
    public async Task<List<int>> GetGameIdsWithoutGoalieShortHandedStats(int seasonStartYear)
    {
        int minId = seasonStartYear * 1_000_000, maxId = (seasonStartYear + 1) * 1_000_000;
        return await _dbContext.GameGoalieStats
            .Where(x => x.GameId >= minId && x.GameId < maxId)
            .GroupBy(x => x.GameId)
            .Where(g => g.Sum(x => x.ShortHandedShotsSaved + x.ShortHandedGoalsAllowed) == 0)
            .Select(g => g.Key)
            .OrderBy(id => id)
            .ToListAsync();
    }

    /// <summary>
    /// Updates only the shorthanded fields of a game's existing goalie rows; everything else is left alone
    /// </summary>
    /// <param name="gameId">Game the stats belong to</param>
    /// <param name="goalieStats">Goalie stats from the game's boxscore</param>
    /// <returns>Number of rows changed</returns>
    public async Task<int> UpdateGoalieShortHandedStats(int gameId, IEnumerable<GameGoalieStats> goalieStats)
    {
        var dbRows = await _dbContext.GameGoalieStats.Where(x => x.GameId == gameId).ToListAsync();
        var changed = 0;
        foreach (var stats in goalieStats)
        {
            var row = dbRows.FirstOrDefault(x => x.PlayerId == stats.PlayerId);
            if (row == null || (row.ShortHandedShotsSaved == stats.ShortHandedShotsSaved
                                && row.ShortHandedGoalsAllowed == stats.ShortHandedGoalsAllowed))
                continue;
            row.ShortHandedShotsSaved = stats.ShortHandedShotsSaved;
            row.ShortHandedGoalsAllowed = stats.ShortHandedGoalsAllowed;
            changed++;
        }
        return changed;
    }

    /// <summary>
    /// Gets a player based on the id
    /// </summary>
    /// <param name="playerId">Id of the player to get</param>
    /// <returns>Desired player</returns>
    private async Task<DbPlayer?> GetDbPlayer(int playerId)
    {
        var dbPlayer = await _dbContext.Player.FirstOrDefaultAsync(x => x.Id == playerId);
        if (dbPlayer == null)
            return null;

        return dbPlayer;
    }
    /// <summary>
    /// Gets a player's draft details based on the id
    /// </summary>
    /// <param name="playerId">Id of the player to get</param>
    /// <returns>Desired player</returns>
    private async Task<DbPlayerDraftDetails?> GetDbPlayerDraftDetails(int playerId)
    {
        var dbPlayerDraftDetails = await _dbContext.PlayerDraftDetails.FirstOrDefaultAsync(x => x.PlayerId == playerId);
        if (dbPlayerDraftDetails == null)
            return null;

        return dbPlayerDraftDetails;
    }
    /// <summary>
    /// Gets a stats for a game
    /// </summary>
    /// <param name="gamePlayerStats">The game player stats</param>
    /// <returns>Desired player</returns>
    private async Task<IDbGamePlayerStats?> GetDbGamePlayerStats(IDbGamePlayerStats gamePlayerStats)
    {
        var dbGoalieStats = await _dbContext.GameGoalieStats.FirstOrDefaultAsync(x => x.GameId == gamePlayerStats.GameId && x.PlayerId == gamePlayerStats.PlayerId);
        var dbSkaterStats = await _dbContext.GameSkaterStats.FirstOrDefaultAsync(x => x.GameId == gamePlayerStats.GameId && x.PlayerId == gamePlayerStats.PlayerId);

        if (dbGoalieStats != null)
            return dbGoalieStats;

        if (dbSkaterStats != null)
            return dbSkaterStats;

        return null;
    }
    /// <summary>
    /// Gets the number of players in the database for a given season.
    /// </summary>
    /// <param name="seasonStartYear">Year to get players for</param>
    /// <returns>Number of players found</returns>
    public async Task<int> GetPlayerStatsCountBySeason(int seasonStartYear)
    {
        var skaterCountTask = _dbContext.GameSkaterStats
            .Include(x => x.Game)
            .Where(y => y.Game != null && y.Game.SeasonStartYear == seasonStartYear)
            .CountAsync();
        var goalieCountTask = _dbContext.GameGoalieStats
            .Include(x => x.Game)
            .Where(y => y.Game != null && y.Game.SeasonStartYear == seasonStartYear)
            .CountAsync();

        await Task.WhenAll(skaterCountTask, goalieCountTask);

        return skaterCountTask.Result + goalieCountTask.Result;
    }

    /// <summary>
    /// Adds or updates player draft details in the database.
    /// </summary>
    /// <param name="players">The players to add draft details for</param>
    /// <returns>None</returns>
    public async Task AddUpdatePlayerDraftDetails(IEnumerable<Player> players)
    {
        var dbPlayersDraftDetails = MapPlayerToDbPlayerDraftDetails.MapList(players);

        var addList = new List<DbPlayerDraftDetails>();
        var updateList = new List<DbPlayerDraftDetails>();
        foreach (var playerDraftDetails in dbPlayersDraftDetails)
        {
            var dbPlayerDraftDetails = await GetDbPlayerDraftDetails(playerDraftDetails.PlayerId);
            if (dbPlayerDraftDetails == null)
            {
                addList.Add(playerDraftDetails);
            }
            else if (!dbPlayerDraftDetails.IsEquivalentTo(playerDraftDetails))
            {
                dbPlayerDraftDetails.Clone(playerDraftDetails);
                updateList.Add(dbPlayerDraftDetails);
            }
        }

        await _dbContext.PlayerDraftDetails.AddRangeAsync(addList);
        _dbContext.PlayerDraftDetails.UpdateRange(updateList);
    }

    /// <summary>
    /// Gets a player by their id.
    /// </summary>
    /// <param name="playerId">Id of the player to get</param>
    /// <returns>The player</returns>
    public async Task<Player?> GetPlayer(int playerId)
    {
        var dbPlayer = await _dbContext.Player.FirstOrDefaultAsync(x => x.Id == playerId);
        if (dbPlayer == null)
            return null;

        return MapDbPlayerToPlayer.Map(dbPlayer);
    }

    /// <summary>
    /// Commits the changes to the database.
    /// </summary>
    public async Task Commit()
    {
        await _dbContext.SaveChangesAsync();
    }
}