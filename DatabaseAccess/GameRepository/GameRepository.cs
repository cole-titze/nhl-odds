using System.Threading.Tasks;
using Entities.DbModels;
using Entities.DbModels.GamePlayEvents;
using Entities.Mappers.GameEventMappers;
using Entities.Mappers.GameMappers;
using Entities.Models;
using Entities.Types;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.GameRepository;

public class GameRepository : IGameRepository
{
    private readonly Dictionary<int, List<DbGameRaw>> _cachedSeasonsGames = new Dictionary<int, List<DbGameRaw>>();
    private readonly Dictionary<int, int> _seasonGameCountCache = new Dictionary<int, int>();
    private readonly NhlDbContext _dbContext;
    private readonly IDictionary<Type, dynamic> _dbSetEventMap;
    public GameRepository(NhlDbContext dbContext)
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
    /// Gets total games for given season in database
    /// </summary>
    /// <param name="seasonStartYear">season start year</param>
    /// <returns>Number of games in the season in the database</returns>
    public async Task<int> GetSavedGameCountForSeason(int seasonStartYear)
    {
        return await _dbContext.GameRaw.Where(s => s.SeasonStartYear == seasonStartYear).CountAsync();
    }

    /// <summary>
    /// Gets total games for given season
    /// </summary>
    /// <param name="seasonStartYear">season start year</param>
    /// <returns>Number of games in the season</returns>
    public async Task<int?> GetGameCountForSeason(int seasonStartYear)
    {
        return await _dbContext.SeasonGameCount.Where(s => s.SeasonId == seasonStartYear)
            .Select(s => s.GameCount)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Updates games to the database and adds them if they don't exist
    /// </summary>
    /// <param name="game">game to add or update</param>
    /// <returns>None</returns>
    public async Task AddUpdateGame(Game game)
    {
        var dbGameToStore = MapGameToDbGame.Map(game);

        var dbGame = await GetDbGame(game.Id);
        if (dbGame == null)
        {
            await _dbContext.GameRaw.AddAsync(dbGameToStore);
        }
        else if (!dbGame.IsEquivalentTo(dbGameToStore))
        {
            dbGame.Clone(dbGameToStore);
            _dbContext.GameRaw.Update(dbGameToStore);
        }
    }

    /// <summary>
    /// Adds or updates the officials for the games.
    /// </summary>
    /// <param name="game">The game that contains official info</param>
    public async Task AddUpdateGameOfficials(Game game)
    {
        var gameOfficials = MapGameToDbGameOfficial.Map(game);

        var addList = new List<DbGameOfficial>();
        var updateList = new List<DbGameOfficial>();
        foreach (var gameOfficial in gameOfficials)
        {
            var dbGameOfficial = await GetDbGameOfficial(gameOfficial.GameId, gameOfficial.Name);
            if (dbGameOfficial == null)
                addList.Add(gameOfficial);
            else if (!dbGameOfficial.IsEquivalentTo(gameOfficial))
            {
                dbGameOfficial.Clone(gameOfficial);
                updateList.Add(dbGameOfficial);
            }
        }

        await _dbContext.GameOfficial.AddRangeAsync(addList);
        _dbContext.GameOfficial.UpdateRange(updateList);
    }

    /// <summary>
    /// Gets a game official from the database based on the game id and official name
    /// </summary>
    /// <param name="gameId">The game id</param>
    /// <param name="officialName">The official name</param>
    /// <returns>The game official object or null if not found</returns>
    private async Task<DbGameOfficial?> GetDbGameOfficial(int gameId, string officialName)
    {
        return await _dbContext.GameOfficial
            .FirstOrDefaultAsync(x => x.GameId == gameId && x.Name == officialName);
    }

    /// <summary>
    /// Adds or updates the officials for the games.
    /// </summary>
    /// <param name="game">The game that contains official info</param>
    public async Task AddUpdateGameCoaches(Game game)
    {
        var gameCoaches = MapGameToDbGameCoaches.Map(game);

        var addList = new List<DbGameCoach>();
        var updateList = new List<DbGameCoach>();
        foreach (var gameCoach in gameCoaches)
        {
            var dbGameCoach = await GetDbGameCoach(gameCoach.GameId, gameCoach.Name);
            if (dbGameCoach == null)
                addList.Add(gameCoach);
            else if (!dbGameCoach.IsEquivalentTo(gameCoach))
            {
                dbGameCoach.Clone(gameCoach);
                updateList.Add(dbGameCoach);
            }
        }

        await _dbContext.GameCoach.AddRangeAsync(addList);
        _dbContext.GameCoach.UpdateRange(updateList);
    }
    /// <summary>
    /// Gets a game coach from the database based on the game id and coach name
    /// </summary>
    /// <param name="gameId">The game id</param>
    /// <param name="name">The coach name</param>
    /// <returns>The game coach object or null if not found</returns>
    private async Task<DbGameCoach?> GetDbGameCoach(int gameId, string name)
    {
        return await _dbContext.GameCoach
            .FirstOrDefaultAsync(x => x.GameId == gameId && x.Name == name);
    }

    /// <summary>
    /// Adds or updates the TV broadcasters for the games.
    /// </summary>
    /// <param name="game">The game that contains broadcaster info</param>
    public async Task AddUpdateGameTvBroadcasters(Game game)
    {
        var gameBroadcasters = MapGameToDbGameTvBroadcasters.Map(game);

        var addList = new List<DbGameTvBroadcaster>();
        var updateList = new List<DbGameTvBroadcaster>();
        foreach (var gameBroadcaster in gameBroadcasters)
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
    /// <param name="tvBroadcasterId"><The broadcaster id/param>
    /// <returns>The game broadcaster object, or null if it doesn't exist</returns>
    private async Task<DbGameTvBroadcaster?> GetDbGameTvBroadcaster(int gameId, int tvBroadcasterId)
    {
        return await _dbContext.GameTvBroadcaster.FirstOrDefaultAsync(x => x.GameId == gameId && x.BroadcasterId == tvBroadcasterId);
    }

    /// <summary>
    /// Gets a seasons worth of games and stores them in the cache variable
    /// </summary>
    /// <param name="seasonStartYear">Season start year</param>
    /// <returns>None</returns>
    private async Task CacheSeasonOfGames(int seasonStartYear)
    {
        if (_cachedSeasonsGames.ContainsKey(seasonStartYear) && _cachedSeasonsGames[seasonStartYear].Count > 0)
            return;

        _cachedSeasonsGames.Clear();
        _cachedSeasonsGames[seasonStartYear] = await _dbContext.GameRaw.Where(s => s.SeasonStartYear == seasonStartYear)
                                    .Include(x => x.AwayTeam)
                                    .Include(x => x.HomeTeam)
                                    .ToListAsync();
    }

    /// <summary>
    /// Gets a seasons worth of games from the database and caches them
    /// </summary>
    /// <param name="seasonStartYear">Season start year</param>
    /// <returns>Seasons games</returns>
    private async Task<IEnumerable<DbGameRaw>> GetSeasonDbGames(int seasonStartYear)
    {
        await CacheSeasonOfGames(seasonStartYear);

        return _cachedSeasonsGames[seasonStartYear];
    }

    /// <summary>
    /// Saves Database changes
    /// </summary>
    /// <returns>None</returns>
    public async Task Commit()
    {
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a game based on the id
    /// </summary>
    /// <param name="gameId">Id of the game to get</param>
    /// <returns>Desired game</returns>
    public async Task<Game?> GetGame(int gameId)
    {
        var dbGame = await GetDbGame(gameId);
        if (dbGame == null)
            return null;

        var game = MapDbGameToGame.Map(dbGame);
        game.ExtendedInfo!.TvBroadcasters = await GetTvBroadcasters(gameId);
        game.RosterStats = await GetGameRosterStats(gameId, dbGame);
        game.GameEvents = MapDbGameEventsToGameEvents.Map(await GetAllDbGameEvents(gameId));

        return game;
    }

    /// <summary>
    /// Gets the roster stats for a specific game
    /// </summary>
    /// <param name="gameId">The game id</param>
    /// <returns>The roster stats for the game</returns>
    private async Task<GameRosterStats?> GetGameRosterStats(int gameId, DbGameRaw game)
    {
        var dbGamePlayerStats = GetDbGamePlayerStats(gameId);
        if (dbGamePlayerStats == null)
            return null;
        var homeTeamDbPlayerStats = dbGamePlayerStats.Where(x => x.TeamId == game.HomeTeamId).ToList();
        var awayTeamDbPlayerStats = dbGamePlayerStats.Where(x => x.TeamId == game.AwayTeamId).ToList();

        var homeSkaters = homeTeamDbPlayerStats.OfType<DbGameSkaterStats>().ToList();
        var homeGoalies = homeTeamDbPlayerStats.OfType<DbGameGoalieStats>().ToList();
        var awaySkaters = awayTeamDbPlayerStats.OfType<DbGameSkaterStats>().ToList();
        var awayGoalies = awayTeamDbPlayerStats.OfType<DbGameGoalieStats>().ToList();

        var homeTeamForwards = homeSkaters.Where(x => x.Position == POSITION.Center || x.Position == POSITION.RightWing || x.Position == POSITION.LeftWing).ToList();
        var homeTeamDefensemen = homeSkaters.Where(x => x.Position == POSITION.Defenseman).ToList();
        var awayTeamForwards = awaySkaters.Where(x => x.Position == POSITION.Center || x.Position == POSITION.RightWing || x.Position == POSITION.LeftWing).ToList();
        var awayTeamDefensemen = awaySkaters.Where(x => x.Position == POSITION.Defenseman).ToList();

        var dbGameOfficials = await GetDbGameOfficials(gameId);
        if (dbGameOfficials == null)
            return null;
        var referees = dbGameOfficials.Where(x => x.Role == Role.Referee).ToList();
        var linesmen = dbGameOfficials.Where(x => x.Role == Role.Linesman).ToList();

        var dbCoaches = await _dbContext.GameCoach
            .Where(x => x.GameId == gameId)
            .ToListAsync();
        var homeTeamCoach = dbCoaches.FirstOrDefault(x => x.TeamId == game.HomeTeamId);
        var awayTeamCoach = dbCoaches.FirstOrDefault(x => x.TeamId == game.AwayTeamId);

        return new GameRosterStats
        {
            HomeTeamCoach = MapDbGameCoachToCoach.Map(homeTeamCoach),
            AwayTeamCoach = MapDbGameCoachToCoach.Map(awayTeamCoach),
            HomeTeamForwards = MapDbGamePlayerStatsToGamePlayerStats.MapSkaterStatsList(homeTeamForwards),
            HomeTeamDefensemen = MapDbGamePlayerStatsToGamePlayerStats.MapSkaterStatsList(homeTeamDefensemen),
            HomeTeamGoalies = MapDbGamePlayerStatsToGamePlayerStats.MapGoalieStatsList(homeGoalies),
            AwayTeamForwards = MapDbGamePlayerStatsToGamePlayerStats.MapSkaterStatsList(awayTeamForwards),
            AwayTeamDefensemen = MapDbGamePlayerStatsToGamePlayerStats.MapSkaterStatsList(awayTeamDefensemen),
            AwayTeamGoalies = MapDbGamePlayerStatsToGamePlayerStats.MapGoalieStatsList(awayGoalies),
            Referees = MapDbGameOfficialToReferee.MapList(referees),
            Linesmen = MapDbGameOfficialToLinesmen.MapList(linesmen)
        };
    }

    /// <summary>
    /// Gets the TV broadcasters for a specific game
    /// </summary>
    /// <param name="gameId">The game Id</param>
    /// <returns>The tv broadcasters</returns>
    private async Task<IEnumerable<TvBroadcaster>> GetTvBroadcasters(int gameId)
    {
        var dbTvBroadcasters = await GetDbTvBroadcasters(gameId);
        return MapDbTvBroadcasterToTvBroadcaster.MapList(dbTvBroadcasters);
    }

    /// <summary>
    /// Gets the TV broadcasters for a specific game
    /// </summary>
    /// <param name="gameId">Game to get broadcasters for</param>
    /// <returns>The broadcasters</returns>
    private async Task<IEnumerable<DbTvBroadcaster>> GetDbTvBroadcasters(int gameId)
    {
        var broadcasters = await _dbContext.GameTvBroadcaster
            .Where(x => x.GameId == gameId && x.Broadcaster != null)
            .Select(x => x.Broadcaster)
            .ToListAsync();

        return broadcasters.Where(b => b != null)!;
    }

    /// <summary>
    /// Gets the game officials for a specific game
    /// </summary>
    /// <param name="gameId">The game to get officials for</param>
    /// <returns>The officials for the game</returns>
    private async Task<IEnumerable<DbGameOfficial>> GetDbGameOfficials(int gameId)
    {
        var officials = await _dbContext.GameOfficial
            .Where(x => x.GameId == gameId)
            .ToListAsync();

        return officials;
    }

    /// <summary>
    /// Get the player stats for a specific game
    /// </summary>
    /// <param name="gameId">The game id</param>
    /// <returns>The player stats</returns>
    private IEnumerable<IDbGamePlayerStats> GetDbGamePlayerStats(int gameId)
    {
        var skaterStatsStats = _dbContext.GameSkaterStats
            .Where(x => x.GameId == gameId)
            .ToList();
        var goalieStats = _dbContext.GameGoalieStats
            .Where(x => x.GameId == gameId)
            .ToList();

        var playerStats = new List<IDbGamePlayerStats>();
        playerStats.AddRange(skaterStatsStats);
        playerStats.AddRange(goalieStats);

        return playerStats;
    }

    /// <summary>
    /// Gets a db game based on the id
    /// </summary>
    /// <param name="gameId">Id of the game to get</param>
    /// <returns>Desired game</returns>
    private async Task<DbGameRaw?> GetDbGame(int gameId)
    {
        // Get the season start year from the game id
        int seasonStartYear = int.Parse(gameId.ToString().Substring(0, 4));
        var seasonGames = await GetSeasonDbGames(seasonStartYear);

        var game = seasonGames.FirstOrDefault(x => x.Id == gameId);
        if (game == null)
            return null;

        return game;
    }

    /// <summary>
    /// Gets the Season game counts. Caches the first call from the database.
    /// </summary>
    /// <returns>Dictionary of season key and game count value</returns>
    public async Task<IDictionary<int, int>> GetSeasonGameCounts()
    {
        if (_seasonGameCountCache.Keys.Count != 0)
            return _seasonGameCountCache;

        var seasonGameCounts = await _dbContext.SeasonGameCount.ToListAsync();

        foreach (var dbGameCount in seasonGameCounts)
        {
            _seasonGameCountCache.Add(dbGameCount.SeasonId, dbGameCount.GameCount);
        }

        return _seasonGameCountCache;
    }

    /// <summary>
    /// Adds the season game counts to the database
    /// </summary>
    /// <param name="seasonGameCountCache">The dictionary of seasonGameCounts to add to the database if they don't exist</param>
    public async Task AddUpdateSeasonGameCount(int seasonStartYear, int seasonGameCount)
    {
        var dbGameCount = await _dbContext.SeasonGameCount.FirstOrDefaultAsync(x => x.SeasonId == seasonStartYear);

        var dbNewSeasonGameCount = new DbSeasonGameCount()
        {
            SeasonId = seasonStartYear,
            GameCount = seasonGameCount
        };
        if (dbGameCount == null)
        {
            await _dbContext.SeasonGameCount.AddAsync(dbNewSeasonGameCount);
        }
        else if (dbGameCount.GameCount != seasonGameCount)
        {
            dbGameCount.Clone(dbNewSeasonGameCount);
            _dbContext.SeasonGameCount.Update(dbNewSeasonGameCount);
        }

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
    private async Task<IEnumerable<IDbGameEvent>> GetAllDbGameEvents(int gameId)
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

