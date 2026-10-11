using System.Net;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameDetailRepository;
using Entities.DbModels;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Fills NhlApiResponse with the raw responses of every played game (boxscore, right-rail, play-by-play, shift chart),
/// every goal replay and every player page, so tables can later be rebuilt without the API (RUN_MODE=Reparse). Only
/// stores: nothing is parsed. Re-runnable: what's already stored is skipped (the cache runs PreferStored). The three
/// hosts run side by side; failures go to ErrorLog.
/// </summary>
public class RawResponseBackfiller
{
    private const int PROGRESS_EVERY = 500;
    private static readonly string[] GAME_CENTER_ENDPOINTS = ["boxscore", "right-rail", "play-by-play"];
    private readonly Func<IGameDetailRepository> _repoFactory;
    private readonly Func<IErrorRepository> _errorRepoFactory;
    private readonly RawResponseCache _rawResponses;
    private readonly ILogger<RawResponseBackfiller> _logger;
    // One client per host, same spacing as the regular getters
    private readonly ThrottledHttpClient _apiWeb = new(TimeSpan.FromMilliseconds(600));
    private readonly ThrottledHttpClient _stats = new(TimeSpan.FromMilliseconds(500));
    private readonly ThrottledHttpClient _replays = new(TimeSpan.FromMilliseconds(250), new Dictionary<string, string>
    {
        ["User-Agent"] = "Mozilla/5.0",
        ["Referer"] = "https://www.nhl.com/",
    });

    public RawResponseBackfiller(Func<IGameDetailRepository> repoFactory, Func<IErrorRepository> errorRepoFactory,
        RawResponseCache rawResponses, ILoggerFactory loggerFactory)
    {
        _repoFactory = repoFactory;
        _errorRepoFactory = errorRepoFactory;
        _rawResponses = rawResponses;
        _logger = loggerFactory.CreateLogger<RawResponseBackfiller>();
    }

    /// <summary>Stores every played game's responses and goal replays season by season, then every player page.
    /// With <paramref name="onlyGameIds"/>, only those games (and no player pages).</summary>
    public async Task Backfill(IReadOnlyCollection<int> seasons, IReadOnlyCollection<int>? onlyGameIds = null)
    {
        foreach (var season in seasons)
        {
            var repo = _repoFactory();
            var games = await repo.GetPlayedGames(season);
            var goals = await repo.GetGoalsWithReplayUrl(season);
            if (onlyGameIds != null)
            {
                games = games.Where(onlyGameIds.Contains).ToList();
                goals = goals.Where(g => onlyGameIds.Contains(g.GameId)).ToList();
            }
            _logger.LogInformation("Raw responses season {Season}: {Games} games, {Goals} goal replays", season, games.Count, goals.Count);
            await Task.WhenAll(
                Store(games.SelectMany(id => GAME_CENTER_ENDPOINTS.Select(e => (id, NhlGameDetailGetter.GameCenterUrl(id, e)))).ToList(),
                    _apiWeb, $"season {season} gamecenter"),
                Store(games.Select(id => (id, NhlGameDetailGetter.ShiftChartUrl(id))).ToList(), _stats, $"season {season} shift charts"),
                Store(goals.Select(g => (g.GameId, g.Url)).ToList(), _replays, $"season {season} goal replays", replays: true));
        }

        if (onlyGameIds == null)
        {
            var players = await _repoFactory().GetPlayerIds();
            await Store(players.Select(id => ((int?)null, $"https://api-web.nhle.com/v1/player/{id}/landing")).ToList(), _apiWeb, "player pages");
        }
    }

    private Task Store(List<(int GameId, string Url)> requests, ThrottledHttpClient client, string label, bool replays = false)
    {
        return Store(requests.Select(r => ((int?)r.GameId, r.Url)).ToList(), client, label, replays);
    }

    private async Task Store(List<(int? GameId, string Url)> requests, ThrottledHttpClient client, string label, bool replays = false)
    {
        var errorRepo = _errorRepoFactory();
        int done = 0, failed = 0;
        foreach (var (gameId, url) in requests)
        {
            try
            {
                var (status, _) = await _rawResponses.Get(url, () => client.Get(url));
                // Replays the NHL doesn't serve (403/404) are expected; GoalReplay records those
                if (status != HttpStatusCode.OK && !(replays && status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound))
                    throw new HttpRequestException($"{url} returned {(int)status} ({status})", null, status);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Raw response failed: {Url}", url);
                await LogError(errorRepo, gameId, ex);
            }
            if (++done % PROGRESS_EVERY == 0)
                _logger.LogInformation("Raw responses {Label}: {Done}/{Total} ({Failed} failed)", label, done, requests.Count, failed);
        }
        _logger.LogInformation("Raw responses {Label}: {Total} done ({Failed} failed)", label, requests.Count, failed);
    }

    private async Task LogError(IErrorRepository errorRepo, int? gameId, Exception ex)
    {
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
                Source = "BackfillRaw",
            });
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "Couldn't write the BackfillRaw error to ErrorLog");
        }
    }
}