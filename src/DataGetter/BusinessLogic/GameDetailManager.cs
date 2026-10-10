using System.Net;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameDetailRepository;
using Entities.DbModels;
using Entities.Types;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Saves the per-game detail the main game fetch doesn't keep: who dressed (rosters with sweater numbers), shift charts
/// (who was on the ice when) and goal tracking replays (player and puck positions before each goal). Games and replays
/// run side by side since they come from different hosts. Failures go to ErrorLog and are retried on the next run;
/// replays the NHL doesn't serve (403/404) are recorded and not retried.
/// </summary>
public class GameDetailManager
{
    private const int PROGRESS_EVERY = 250;
    private readonly Func<IGameDetailRepository> _repoFactory;
    private readonly Func<IErrorRepository> _errorRepoFactory;
    private readonly INhlGameDetailGetter _getter;
    private readonly ILogger<GameDetailManager> _logger;

    public GameDetailManager(Func<IGameDetailRepository> repoFactory, Func<IErrorRepository> errorRepoFactory,
        INhlGameDetailGetter getter, ILoggerFactory loggerFactory)
    {
        _repoFactory = repoFactory;
        _errorRepoFactory = errorRepoFactory;
        _getter = getter;
        _logger = loggerFactory.CreateLogger<GameDetailManager>();
    }

    /// <summary>
    /// The nightly run: re-fetches rosters and shifts for games played in the last <paramref name="refetchDays"/> days
    /// (shift charts get corrected like box scores), fetches any this season still missing or empty, and every new goal
    /// replay.
    /// </summary>
    public async Task FetchRecent(int seasonStartYear, IEnumerable<int> recentGameIds)
    {
        var repo = _repoFactory();
        var gameIds = recentGameIds
            .Union(await repo.GetPlayedGamesWithoutFetch(seasonStartYear, "Roster"))
            // The NHL posts some shift charts late, so this season's empty ones are tried again
            .Union(await repo.GetPlayedGamesWithoutFetch(seasonStartYear, "Shifts", includeEmpty: true))
            .Order()
            .ToList();
        var goals = await repo.GetGoalsWithoutReplay(seasonStartYear);
        _logger.LogInformation("Game details: {Games} game(s), {Goals} goal replay(s) to fetch", gameIds.Count, goals.Count);
        await Task.WhenAll(FetchGames(gameIds, $"season {seasonStartYear}"), FetchReplays(goals, $"season {seasonStartYear}"));
    }

    /// <summary>
    /// Fetches every played game's roster and shifts and every goal replay not saved yet, season by season.
    /// Re-runnable: what's already saved is skipped.
    /// </summary>
    public async Task Backfill(YearRange seasons)
    {
        var years = Enumerable.Range(seasons.StartYear, seasons.EndYear - seasons.StartYear + 1).ToList();
        await Task.WhenAll(
            Task.Run(async () =>
            {
                foreach (var year in years)
                {
                    var repo = _repoFactory();
                    var gameIds = (await repo.GetPlayedGamesWithoutFetch(year, "Roster"))
                        .Union(await repo.GetPlayedGamesWithoutFetch(year, "Shifts"))
                        .Order()
                        .ToList();
                    await FetchGames(gameIds, $"season {year}");
                }
            }),
            Task.Run(async () =>
            {
                foreach (var year in years)
                    await FetchReplays(await _repoFactory().GetGoalsWithoutReplay(year), $"season {year}");
            }));
    }

    private async Task FetchGames(List<int> gameIds, string label)
    {
        if (gameIds.Count == 0)
            return;
        var repo = _repoFactory();
        var errorRepo = _errorRepoFactory();
        int done = 0, failed = 0;
        foreach (var gameId in gameIds)
        {
            var fetchedUtc = DateTime.UtcNow;
            var roster = Fetch(() => _getter.GetPlayByPlay(gameId));
            var shifts = Fetch(() => _getter.GetShiftChart(gameId));
            var ok = await Save(errorRepo, gameId, "GameDetails Roster", async () =>
                await repo.ReplaceRoster(gameId, GameDetailParser.ParseRosterSpots(gameId, await roster), fetchedUtc));
            ok &= await Save(errorRepo, gameId, "GameDetails Shifts", async () =>
                await repo.ReplaceShifts(gameId, GameDetailParser.ParseShifts(gameId, await shifts), fetchedUtc));
            failed += ok ? 0 : 1;
            if (++done % PROGRESS_EVERY == 0)
                _logger.LogInformation("Game details {Label}: {Done}/{Total} games ({Failed} with errors)", label, done, gameIds.Count, failed);
        }
        _logger.LogInformation("Game details {Label}: {Total} games done ({Failed} with errors)", label, gameIds.Count, failed);
    }

    private async Task FetchReplays(List<(int GameId, int EventId, string Url)> goals, string label)
    {
        if (goals.Count == 0)
            return;
        var repo = _repoFactory();
        var errorRepo = _errorRepoFactory();
        int done = 0, failed = 0, unavailable = 0;
        foreach (var (gameId, eventId, url) in goals)
        {
            var ok = await Save(errorRepo, gameId, "GameDetails Replay", async () =>
            {
                var (status, body) = await _getter.GetGoalReplay(url);
                if (status != HttpStatusCode.OK && status != HttpStatusCode.Forbidden && status != HttpStatusCode.NotFound)
                    throw new HttpRequestException($"{url} returned {(int)status} ({status})", null, status);
                var positions = body != null ? GameDetailParser.ParseReplay(gameId, eventId, body) : [];
                if (body == null)
                    unavailable++;
                await repo.ReplaceReplay(new DbGoalReplay
                {
                    GameId = gameId,
                    EventId = eventId,
                    HttpStatus = (short)status,
                    FrameCount = positions.Count == 0 ? (short)0 : (short)(positions.Max(p => p.Frame) + 1),
                    FirstTimeStamp = positions.Count == 0 ? null : positions.Min(p => p.TimeStamp),
                    FetchedUTC = DateTime.UtcNow,
                }, positions);
            });
            failed += ok ? 0 : 1;
            if (++done % PROGRESS_EVERY == 0)
                _logger.LogInformation("Goal replays {Label}: {Done}/{Total} ({Unavailable} not served, {Failed} errors)",
                    label, done, goals.Count, unavailable, failed);
        }
        _logger.LogInformation("Goal replays {Label}: {Total} done ({Unavailable} not served, {Failed} errors)",
            label, goals.Count, unavailable, failed);
    }

    // Starts the request now so the play-by-play and shift chart requests (different hosts) overlap
    private static Task<string> Fetch(Func<Task<string>> request) => Task.Run(request);

    private async Task<bool> Save(IErrorRepository errorRepo, int gameId, string source, Func<Task> work)
    {
        try
        {
            await work();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Source} failed for game {GameId}", source, gameId);
            try
            {
                await errorRepo.AddError(new DbErrorLog
                {
                    TimestampUTC = DateTime.UtcNow,
                    GameId = gameId,
                    SeasonStartYear = gameId / 1_000_000,
                    ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace ?? string.Empty,
                    Source = source,
                });
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Couldn't write the {Source} error to ErrorLog", source);
            }
            return false;
        }
    }
}