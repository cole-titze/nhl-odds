using System.Diagnostics;
using DatabaseAccess;
using DatabaseAccess.CleanedGameRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameEventSeasonRepository;
using DatabaseAccess.GameSeasonRepository;
using DatabaseAccess.PlayerStatsSeasonRepository;
using DataCleaner.Mappers;
using Entities.DbModels;
using Entities.Models;
using Entities.Types;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;

namespace DataCleaner;

public class GameCleaner
{
    private readonly Func<NhlDbContext> _createDbContext;
    private readonly int _maxParallelSeasons;
    private readonly ILogger<GameCleaner> _logger;

    /// <param name="createDbContext">Makes a fresh DbContext. Each season gets its own, since a DbContext
    /// can't be shared between threads.</param>
    /// <param name="maxParallelSeasons">How many seasons to clean at once. Each one holds two seasons of
    /// games, player stats and events in memory.</param>
    public GameCleaner(Func<NhlDbContext> createDbContext, ILoggerFactory loggerFactory, int maxParallelSeasons = 1)
    {
        _createDbContext = createDbContext;
        _maxParallelSeasons = Math.Max(1, maxParallelSeasons);
        _logger = loggerFactory.CreateLogger<GameCleaner>();
    }

    /// <summary>
    /// Builds an error message that includes the innermost exception's detail (e.g. the actual
    /// Postgres constraint error inside a DbUpdateException, which ex.Message alone doesn't show).
    /// Bounded so a pathological exception chain can't write an unbounded blob into ErrorLog.
    /// </summary>
    private static string BuildErrorMessage(Exception ex, int maxInnerLength = 1000)
    {
        var inner = ex.InnerException;
        var depth = 0;
        while (inner?.InnerException != null && depth < 5)
        {
            inner = inner.InnerException;
            depth++;
        }

        if (inner == null)
            return ex.Message;

        var innerDetail = $"{inner.GetType().Name}: {inner.Message}";
        if (innerDetail.Length > maxInnerLength)
            innerDetail = innerDetail[..maxInnerLength];

        return $"{ex.Message} | Inner: {innerDetail}";
    }

    /// <param name="cleanAll">Re-clean every game, not just new and future games of past seasons. Needed after
    /// a change to how features are computed.</param>
    public async Task CleanGamesInSeasons(YearRange seasonYearRange, bool cleanAll = false)
    {
        // A season reads only its own and the previous season's raw data, so seasons don't depend on each other
        var seasons = Enumerable.Range(seasonYearRange.StartYear, seasonYearRange.EndYear - seasonYearRange.StartYear + 1);
        await Parallel.ForEachAsync(seasons, new ParallelOptions { MaxDegreeOfParallelism = _maxParallelSeasons },
            async (seasonStartYear, _) => await CleanSeason(seasonStartYear, seasonStartYear == seasonYearRange.EndYear, cleanAll));
    }

    private async Task CleanSeason(int seasonStartYear, bool isLastSeason, bool cleanAll)
    {
        var watch = Stopwatch.StartNew();
        // Errors get their own context so clearing the cleaned-game tracking after a failure doesn't drop them
        await using var dbContext = _createDbContext();
        await using var errorDbContext = _createDbContext();
        var gameRepo = new GameSeasonRepository(dbContext);
        var cleanedGameRepo = new CleanedGameRepository(dbContext);
        var playerStatsRepo = new PlayerStatsSeasonRepository(dbContext);
        var gameEventSeasonRepo = new GameEventSeasonRepository(dbContext);
        var errorRepo = new ErrorRepository(errorDbContext);

        var seasonGames = (await gameRepo.GetSeasonGames(seasonStartYear))
            .Where(g => g.GameType == GameType.Regular)
            .ToList();
        var gamesToClean = (IEnumerable<Game>)seasonGames;
        var existingCleanedGames = await cleanedGameRepo.GetSeasonOfCleanedGames(seasonStartYear);

        if (!isLastSeason && !cleanAll)
            gamesToClean = GetGamesToClean(existingCleanedGames, seasonGames);

        if (!gamesToClean.Any())
        {
            _logger.LogInformation("All game data for season " + seasonStartYear.ToString() + " already exists. Skipping...");
            return;
        }

        var lastSeasonGames = (await gameRepo.GetSeasonGames(seasonStartYear - 1))
            .Where(g => g.GameType == GameType.Regular)
            .ToList();
        var gameMap = new SeasonGames(seasonGames.Concat(lastSeasonGames));

        // Load player stats for current + previous season to build roster scorer
        var currentSkaterStats = await playerStatsRepo.GetSeasonSkaterStats(seasonStartYear);
        var lastSkaterStats = await playerStatsRepo.GetSeasonSkaterStats(seasonStartYear - 1);
        var currentGoalieStats = await playerStatsRepo.GetSeasonGoalieStats(seasonStartYear);
        var lastGoalieStats = await playerStatsRepo.GetSeasonGoalieStats(seasonStartYear - 1);

        // Build game list from the raw games for date lookup
        var allGamesForScorer = seasonGames.Concat(lastSeasonGames)
            .Select(g => new DbGameRaw { Id = g.Id, GameDateUTC = g.GameDateUTC, HomeTeamId = g.HomeTeamId, AwayTeamId = g.AwayTeamId });

        // Roster report snapshots covering this season's games (empty before snapshots were collected)
        var rosterStatuses = await playerStatsRepo.GetRosterStatuses(
            new DateTime(seasonStartYear, 7, 1), new DateTime(seasonStartYear + 1, 7, 1));

        var rosterScorer = new RosterScorer(
            currentSkaterStats.Concat(lastSkaterStats),
            currentGoalieStats.Concat(lastGoalieStats),
            allGamesForScorer,
            rosterStatuses);

        // Load event data for current + previous season to build event aggregator
        var currentPenalties = await gameEventSeasonRepo.GetSeasonPenalties(seasonStartYear);
        var lastPenalties = await gameEventSeasonRepo.GetSeasonPenalties(seasonStartYear - 1);
        var currentGoals = await gameEventSeasonRepo.GetSeasonGoals(seasonStartYear);
        var lastGoals = await gameEventSeasonRepo.GetSeasonGoals(seasonStartYear - 1);
        var currentFaceoffs = await gameEventSeasonRepo.GetSeasonFaceoffs(seasonStartYear);
        var lastFaceoffs = await gameEventSeasonRepo.GetSeasonFaceoffs(seasonStartYear - 1);
        var currentMissedShots = await gameEventSeasonRepo.GetSeasonMissedShots(seasonStartYear);
        var lastMissedShots = await gameEventSeasonRepo.GetSeasonMissedShots(seasonStartYear - 1);

        var eventAggregator = new EventAggregator(
            currentPenalties.Concat(lastPenalties),
            currentGoals.Concat(lastGoals),
            currentFaceoffs.Concat(lastFaceoffs),
            currentMissedShots.Concat(lastMissedShots),
            seasonGames.Concat(lastSeasonGames));

        const int batchSize = 200;
        var cleanedGames = new List<DbGameCleaned>();
        int totalCleaned = 0;
        foreach (var game in gamesToClean)
        {
            try
            {
                var cleanedGame = MapGameToDbGameCleaned.Map(game, gameMap, rosterScorer);
                MapEventToDbGameCleaned.Apply(cleanedGame, eventAggregator, game);
                cleanedGames.Add(cleanedGame);

                if (cleanedGames.Count >= batchSize)
                {
                    await cleanedGameRepo.AddUpdateCleanedGames(cleanedGames);
                    await cleanedGameRepo.Commit();
                    totalCleaned += cleanedGames.Count;
                    cleanedGames.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning game {GameId} in season {Season}. Skipping.", game.Id, seasonStartYear);
                cleanedGames.Clear();
                cleanedGameRepo.ClearTracking();
                var stackTrace = ex.StackTrace ?? string.Empty;
                var stackFrames = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var topFrames = string.Join(" | ", stackFrames.Take(10).Select(f => f.Trim()));
                await errorRepo.AddError(new DbErrorLog
                {
                    TimestampUTC = DateTime.UtcNow,
                    GameId = game.Id,
                    SeasonStartYear = seasonStartYear,
                    ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                    Message = BuildErrorMessage(ex),
                    StackTrace = topFrames,
                    Source = "GameCleaner.CleanGamesInSeasons"
                });
            }
        }

        if (cleanedGames.Count > 0)
        {
            try
            {
                await cleanedGameRepo.AddUpdateCleanedGames(cleanedGames);
                await cleanedGameRepo.Commit();
                totalCleaned += cleanedGames.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving final batch for season {Season}.", seasonStartYear);
            }
        }
        _logger.LogInformation("Number of Games Added To Season {Season}: {Count} ({Seconds:F0}s)",
            seasonStartYear, totalCleaned, watch.Elapsed.TotalSeconds);
    }

    private static IEnumerable<Game> GetGamesToClean(IEnumerable<DbGameCleaned> cleanedGames, IEnumerable<Game> games)
    {
        var gamesToClean = new List<Game>();
        gamesToClean.AddRange(games.GetFutureGames());
        gamesToClean.AddRange(cleanedGames.GetNewGames(games));

        return gamesToClean.GetUniqueGames();
    }
}