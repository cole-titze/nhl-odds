using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using Entities.ServiceModels.OddsApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.OddsApi;

namespace BookmakerOddsGetter;

public class BookmakerOddsBackfiller
{
    private const int BACKFILL_START_YEAR = 2024;
    private readonly NhlDbContext _dbContext;
    private readonly IBookmakerOddsRepository _bookmakerOddsRepo;
    private readonly IOddsApiGetter _oddsApiGetter;
    private readonly ILogger<BookmakerOddsBackfiller> _logger;

    public BookmakerOddsBackfiller(
        NhlDbContext dbContext,
        IBookmakerOddsRepository bookmakerOddsRepo,
        IOddsApiGetter oddsApiGetter,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _bookmakerOddsRepo = bookmakerOddsRepo;
        _oddsApiGetter = oddsApiGetter;
        _logger = loggerFactory.CreateLogger<BookmakerOddsBackfiller>();
    }

    public async Task BackfillBookmakerOdds()
    {
        var seasons = await _dbContext.GameRaw
            .Where(g => g.SeasonStartYear >= BACKFILL_START_YEAR)
            .Select(g => g.SeasonStartYear)
            .Distinct()
            .OrderBy(y => y)
            .ToListAsync();

        var allSeasonTeams = await _dbContext.SeasonTeam
            .Where(t => t.SeasonStartYear >= BACKFILL_START_YEAR)
            .ToListAsync();
        var seasonTeamsByYear = allSeasonTeams
            .GroupBy(t => t.SeasonStartYear)
            .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.TeamId, t => t.Name));

        foreach (var season in seasons)
        {
            var totalGames = await _bookmakerOddsRepo.GetGameCountForSeason(season);
            var gamesWithOdds = await _bookmakerOddsRepo.GetGamesWithOddsCountForSeason(season);

            if (totalGames > 0 && gamesWithOdds >= totalGames)
            {
                _logger.LogInformation("Season {Season}: all {Count} games have bookmaker odds, skipping.", season, totalGames);
                continue;
            }

            _logger.LogInformation("Season {Season}: {With}/{Total} games have bookmaker odds, backfilling...",
                season, gamesWithOdds, totalGames);

            await BackfillSeason(season, seasonTeamsByYear);
        }

        _logger.LogInformation("Backfill complete.");
    }

    private async Task BackfillSeason(int seasonStartYear, Dictionary<int, Dictionary<int, string>> seasonTeamsByYear)
    {
        var gameDates = await _dbContext.GameRaw
            .Where(g => g.SeasonStartYear == seasonStartYear)
            .Select(g => g.GameDateUTC.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var existingGameIds = await _bookmakerOddsRepo.GetGameIdsWithBookmakerOdds();

        if (!seasonTeamsByYear.TryGetValue(seasonStartYear, out var seasonTeams))
        {
            _logger.LogWarning("No season teams found for {Year}, skipping.", seasonStartYear);
            return;
        }

        foreach (var gameDate in gameDates)
        {
            // Include next day — evening NA games have next-day UTC dates
            var nextDate = gameDate.AddDays(1);
            var gamesOnDate = await _dbContext.GameRaw
                .Where(g => (g.GameDateUTC.Date == gameDate || g.GameDateUTC.Date == nextDate)
                    && g.SeasonStartYear == seasonStartYear)
                .Select(g => new { g.Id, g.HomeTeamId, g.AwayTeamId, g.GameDateUTC })
                .ToListAsync();

            var gamesNeedingOdds = gamesOnDate.Where(g => !existingGameIds.Contains(g.Id)).ToList();
            if (!gamesNeedingOdds.Any())
                continue;

            var dateGameDates = gamesOnDate.ToDictionary(g => g.Id, g => g.GameDateUTC);
            var gameInfoList = BookmakerOddsHelper.BuildGameInfoList(
                gamesNeedingOdds.Select(g => new GameRef(g.Id, g.HomeTeamId, g.AwayTeamId)),
                seasonTeams, dateGameDates);

            if (!gameInfoList.Any())
            {
                _logger.LogWarning("No game info could be built for date {Date}", gameDate.ToString("yyyy-MM-dd"));
                continue;
            }

            // Query at 10pm Central the night before (mimics daily job timing)
            var centralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
            var centralPriorNight = new DateTime(gameDate.Year, gameDate.Month, gameDate.Day, 22, 0, 0).AddDays(-1);
            var queryDate = TimeZoneInfo.ConvertTimeToUtc(centralPriorNight, centralZone);
            var responses = await GetOrFetchResponses(queryDate, gameDate);

            if (responses == null)
                continue;

            _logger.LogInformation("Date {Date}: {GameCount} DB games, {ResponseCount} API games",
                gameDate.ToString("yyyy-MM-dd"), gameInfoList.Count, responses.Count);

            await BookmakerOddsHelper.MapAndSave(_logger, _bookmakerOddsRepo, responses, gameInfoList);

            foreach (var g in gamesNeedingOdds)
                existingGameIds.Add(g.Id);
        }
    }

    private async Task<List<OddsApiResponse>?> GetOrFetchResponses(DateTime queryDate, DateTime gameDate)
    {
        var cached = await _bookmakerOddsRepo.GetCachedResponse(queryDate);

        if (cached != null)
        {
            _logger.LogInformation("Using cached response for {Date}", gameDate.ToString("yyyy-MM-dd"));

            // Cached response could be historical format ({data: [...]}) or flat array format
            var trimmed = cached.RawJson.TrimStart();
            if (trimmed.StartsWith('['))
            {
                return JsonConvert.DeserializeObject<List<OddsApiResponse>>(cached.RawJson)
                    ?? new List<OddsApiResponse>();
            }

            var wrapper = JsonConvert.DeserializeObject<OddsApiHistoricalResponse>(cached.RawJson);
            return wrapper?.Data ?? new List<OddsApiResponse>();
        }

        _logger.LogInformation("Fetching historical odds for {Date}...", gameDate.ToString("yyyy-MM-dd"));
        var apiResult = await _oddsApiGetter.GetHistoricalOdds(queryDate);

        if (!apiResult.Responses.Any())
        {
            _logger.LogWarning("No historical odds returned for {Date}", gameDate.ToString("yyyy-MM-dd"));
            return null;
        }

        await _bookmakerOddsRepo.SaveRawResponse(apiResult.RawJson, queryDate);
        await _bookmakerOddsRepo.Commit();

        var remaining = _oddsApiGetter.GetRemainingRequests();
        if (remaining.HasValue)
            _logger.LogInformation("Odds API requests remaining: {Remaining}", remaining.Value);

        return apiResult.Responses;
    }
}
