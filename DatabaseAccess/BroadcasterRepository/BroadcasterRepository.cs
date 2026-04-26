using Entities.DbModels;
using Entities.Mappers.GameMappers;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.BroadcasterRepository;

public class BroadcasterRepository : IBroadcasterRepository
{
    private readonly NhlDbContext _dbContext;

    public BroadcasterRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Adds or updates the TV broadcasters for the games.
    /// </summary>
    /// <param name="game">The game to add broadcasters for</param>
    public async Task AddUpdateTvBroadcasters(Game game)
    {
        var dbTvBroadcasters = MapGameToDbTvBroadcasters.Map(game);
        var uniqueTvBroadcasters = dbTvBroadcasters.GroupBy(b => b.Id).Select(g => g.First()).ToList();

        var addList = new List<DbTvBroadcaster>();
        var updateList = new List<DbTvBroadcaster>();
        foreach (var broadcaster in uniqueTvBroadcasters)
        {
            var dbTvBroadcaster = await GetDbTvBroadcaster(broadcaster.Id);
            if (dbTvBroadcaster == null)
                addList.Add(broadcaster);
            else if (!dbTvBroadcaster.IsEquivalentTo(broadcaster))
            {
                dbTvBroadcaster.Clone(broadcaster);
                updateList.Add(dbTvBroadcaster);
            }
        }

        await _dbContext.TvBroadcaster.AddRangeAsync(addList);
        _dbContext.TvBroadcaster.UpdateRange(updateList);
    }

    /// <summary>
    /// Adds or updates the TV broadcasters for the games.
    /// </summary>
    /// <param name="game">The game that contains broadcaster info</param>
    public async Task AddUpdateGameTvBroadcasters(Game game)
    {
        var gameBroadcasters = MapGameToDbGameTvBroadcasters.Map(game);
        var uniqueGameBroadcasters = gameBroadcasters.GroupBy(b => new { b.GameId, b.BroadcasterId }).Select(g => g.First()).ToList();

        var addList = new List<DbGameTvBroadcaster>();
        var updateList = new List<DbGameTvBroadcaster>();
        foreach (var gameBroadcaster in uniqueGameBroadcasters)
        {
            var dbTvBroadcaster = await GetDbGameTvBroadcaster(gameBroadcaster.GameId, gameBroadcaster.BroadcasterId);
            if (dbTvBroadcaster == null)
                addList.Add(gameBroadcaster);
            else if (!dbTvBroadcaster.IsEquivalentTo(gameBroadcaster))
            {
                dbTvBroadcaster.Clone(gameBroadcaster);
                updateList.Add(dbTvBroadcaster);
            }
        }

        await _dbContext.GameTvBroadcaster.AddRangeAsync(addList);
        _dbContext.GameTvBroadcaster.UpdateRange(updateList);
    }

    /// <summary>
    /// Gets a TV broadcaster from the database based on the id
    /// </summary>
    /// <param name="id">The tv broadcaster id</param>
    /// <returns>Tv broadcaster or null if it doesn't exist</returns>
    private async Task<DbTvBroadcaster?> GetDbTvBroadcaster(int id)
    {
        return await _dbContext.TvBroadcaster.FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    /// Gets a game TV broadcaster from the database based on the game id and tv broadcaster id
    /// </summary>
    /// <param name="gameId">The game Id</param>
    /// <param name="tvBroadcasterId">The broadcaster id</param>
    /// <returns>The game broadcaster object, or null if it doesn't exist</returns>
    private async Task<DbGameTvBroadcaster?> GetDbGameTvBroadcaster(int gameId, int tvBroadcasterId)
    {
        return await _dbContext.GameTvBroadcaster.FirstOrDefaultAsync(x => x.GameId == gameId && x.BroadcasterId == tvBroadcasterId);
    }
}