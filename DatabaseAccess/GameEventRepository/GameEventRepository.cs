using Entities.DbModels;
using Entities.DbModels.GamePlayEvents;
using Entities.Mappers.GameMappers;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.GameEventRepository;

public class GameEventRepository : IGameEventRepository
{
    private readonly NhlDbContext _dbContext;
    private readonly IDictionary<Type, dynamic> _dbSetEventMap;

    public GameEventRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSetEventMap = new Dictionary<Type, dynamic>
        {
            { typeof(DbBlockedShot), _dbContext.GameBlockedShotEvent },
            { typeof(DbGoal), _dbContext.GameGoalEvent },
            { typeof(DbPenalty), _dbContext.GamePenaltyEvent },
            { typeof(DbFaceoff), _dbContext.GameFaceoffEvent },
            { typeof(DbGiveaway), _dbContext.GameGiveawayEvent },
            { typeof(DbHit), _dbContext.GameHitEvent },
            { typeof(DbMissedShot), _dbContext.GameMissedShotEvent },
            { typeof(DbTakeaway), _dbContext.GameTakeawayEvent },
            { typeof(DbShot), _dbContext.GameShotEvent },
            { typeof(DbDelayedPenalty), _dbContext.GameDelayedPenaltyEvent },
            { typeof(DbGameEnd), _dbContext.GameGameEndEvent },
            { typeof(DbPeriodStart), _dbContext.GamePeriodStartEvent },
            { typeof(DbStoppage), _dbContext.GameStoppageEvent },
            { typeof(DbPeriodEnd), _dbContext.GamePeriodEndEvent },
            { typeof(DbShootoutComplete), _dbContext.GameShootoutCompleteEvent },
        };
    }

    /// <summary>
    /// Adds/updates the game events
    /// </summary>
    /// <param name="game">The game to store the events of</param>
    /// <returns>None</returns>
    public async Task AddUpdateGameEvents(Game game)
    {
        var dbGameEvents = MapGameToDbGameEvent.Map(game);

        var addList = new List<IDbGameEvent>();
        var updateList = new List<IDbGameEvent>();
        foreach (var gameEvent in dbGameEvents)
        {
            var dbGameEvent = await GetDbGameEvent(gameEvent);
            if (dbGameEvent == null)
            {
                addList.Add(gameEvent);
            }
            else if (!dbGameEvent.IsEquivalentTo(gameEvent))
            {
                dbGameEvent.Clone(gameEvent);
                updateList.Add(dbGameEvent);
            }
        }

        // Type-safe, clean, no reflection
        await AddOrUpdateEvents(_dbContext.GameBlockedShotEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameGoalEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GamePenaltyEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameFaceoffEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameGiveawayEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameHitEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameMissedShotEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameTakeawayEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameShotEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameDelayedPenaltyEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameGameEndEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GamePeriodStartEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameStoppageEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GamePeriodEndEvent, addList, updateList);
    }

    /// <summary>
    /// Adds or updates the events
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    /// <param name="dbSet">the db object to use</param>
    /// <param name="addList">List of events to add</param>
    /// <param name="updateList">List of events to update</param>
    /// <returns>None</returns>
    private async Task AddOrUpdateEvents<T>(DbSet<T> dbSet, IEnumerable<IDbGameEvent> addList, IEnumerable<IDbGameEvent> updateList) where T : class, IDbGameEvent
    {
        var typedAdd = addList.OfType<T>().ToList();
        var typedUpdate = updateList.OfType<T>().ToList();

        if (typedAdd.Any())
        {
            await dbSet.AddRangeAsync(typedAdd);
        }

        if (typedUpdate.Any())
        {
            dbSet.UpdateRange(typedUpdate);
        }
    }

    /// <summary>
    /// Gets all game events for a given game from all event tables
    /// </summary>
    /// <param name="gameId">The game to get events for</param>
    /// <returns>All events for the game</returns>
    public async Task<IEnumerable<IDbGameEvent>> GetAllDbGameEvents(int gameId)
    {
        var events = new List<IDbGameEvent>();
        foreach (var kvp in _dbSetEventMap)
        {
            var dbSet = kvp.Value as IQueryable<IDbGameEvent> ?? ((IQueryable)kvp.Value).Cast<IDbGameEvent>();
            var gameEvents = await dbSet.Where(x => x.GameId == gameId).ToListAsync();
            events.AddRange(gameEvents);
        }
        return events;
    }

    /// <summary>
    /// Gets a game event from the database
    /// </summary>
    /// <param name="gameEvent">The game event to get</param>
    /// <returns>Desired game event</returns>
    private async Task<IDbGameEvent?> GetDbGameEvent(IDbGameEvent gameEvent)
    {
        int gameId = gameEvent.GameId;
        int id = gameEvent.Id;

        foreach (var kvp in _dbSetEventMap)
        {
            var type = kvp.Key;
            var dbSet = kvp.Value as IQueryable<IDbGameEvent> ?? ((IQueryable)kvp.Value).Cast<IDbGameEvent>();
            if (!type.IsInstanceOfType(gameEvent))
                continue;

            var entity = await dbSet.FirstOrDefaultAsync(x => x.GameId == gameId && x.Id == id);

            if (entity != null)
                return entity;
        }

        return null;
    }
}
