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
            { typeof(DbFailedShotAttempt), _dbContext.GameFailedShotAttemptEvent },
        };
    }

    /// <summary>
    /// Adds/updates the game events
    /// </summary>
    /// <param name="game">The game to store the events of</param>
    /// <returns>None</returns>
    public async Task AddUpdateGameEvents(Game game)
    {
        var dbGameEvents = MapGameToDbGameEvent.Map(game).ToList();

        // Batch-load all existing events for this game in one pass (one query per event type)
        var existingEvents = await GetAllDbGameEvents(game.Id);
        var existingByTypeAndId = existingEvents
            .ToDictionary(e => (e.GetType(), e.Id));

        var addList = new List<IDbGameEvent>();
        var updateList = new List<IDbGameEvent>();
        foreach (var gameEvent in dbGameEvents)
        {
            var key = (gameEvent.GetType(), gameEvent.Id);
            if (!existingByTypeAndId.TryGetValue(key, out var dbGameEvent))
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
        await AddOrUpdateEvents(_dbContext.GameShootoutCompleteEvent, addList, updateList);
        await AddOrUpdateEvents(_dbContext.GameFailedShotAttemptEvent, addList, updateList);

        // Only called for played games, so the play-by-play is authoritative: drop events the NHL has since
        // removed or re-typed (a shot re-scored as a miss keeps its event id, so the old row would count it twice)
        _dbContext.RemoveRange(GetStaleEvents(existingEvents, dbGameEvents, game.GameEvents!.SourceEventIds));
    }

    /// <summary>
    /// Gets the stored events the NHL has removed from the game's play-by-play or re-typed. An event that is still in
    /// the play-by-play but failed to map is kept, since the mapper skips plays it can't read. Returns none when the
    /// new play-by-play has fewer than half the stored events, so a truncated API response can't wipe a game.
    /// </summary>
    /// <param name="existingEvents">The game's stored events</param>
    /// <param name="newEvents">The game's mapped events from the latest play-by-play</param>
    /// <param name="sourceEventIds">Ids of every play in the latest play-by-play, mapped or not</param>
    /// <returns>Events to delete</returns>
    public static IEnumerable<IDbGameEvent> GetStaleEvents(IEnumerable<IDbGameEvent> existingEvents, IEnumerable<IDbGameEvent> newEvents, IReadOnlySet<int> sourceEventIds)
    {
        var existing = existingEvents.ToList();
        var newList = newEvents.ToList();
        var newKeys = newList.Select(e => (e.GetType(), e.Id)).ToHashSet();
        var newIds = newList.Select(e => e.Id).ToHashSet();
        if (sourceEventIds.Count * 2 < existing.Count)
            return [];
        return existing
            .Where(e => !newKeys.Contains((e.GetType(), e.Id)))
            .Where(e => !sourceEventIds.Contains(e.Id) || newIds.Contains(e.Id))
            .ToList();
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