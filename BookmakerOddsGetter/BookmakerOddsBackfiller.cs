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
    private const int BACKFILL_START_YEAR = 2009;
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

        try
        {
            foreach (var season in seasons)
            {
                _logger.LogInformation("Season {Season}: backfilling odds...", season);
                await BackfillSeason(season, seasonTeamsByYear);
            }

            _logger.LogInformation("Backfill complete.");
        }
        catch (OddsApiRateLimitException)
        {
            _logger.LogWarning("Rate limited by Odds API (429). Stopping backfill — progress has been saved.");
        }
    }

    private async Task BackfillSeason(int seasonStartYear, Dictionary<int, Dictionary<int, string>> seasonTeamsByYear)
    {
        var centralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

        // Get all games and group by Central time date (not UTC) so evening NA games
        // land on the correct local date instead of the next UTC day
        var allGames = await _dbContext.GameRaw
            .Where(g => g.SeasonStartYear == seasonStartYear)
            .Select(g => new { g.Id, g.HomeTeamId, g.AwayTeamId, g.GameDateUTC })
            .ToListAsync();

        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, centralZone).Date;
        var gameDates = allGames
            .Select(g => TimeZoneInfo.ConvertTimeFromUtc(g.GameDateUTC, centralZone).Date)
            .Where(d => d < today)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (!seasonTeamsByYear.TryGetValue(seasonStartYear, out var seasonTeams))
        {
            _logger.LogWarning("No season teams found for {Year}, skipping.", seasonStartYear);
            return;
        }

        foreach (var gameDate in gameDates)
        {
            // Find games whose Central time date matches this game date
            var gamesOnDate = allGames
                .Where(g => TimeZoneInfo.ConvertTimeFromUtc(g.GameDateUTC, centralZone).Date == gameDate)
                .ToList();

            var dateGameDates = gamesOnDate.ToDictionary(g => g.Id, g => g.GameDateUTC);
            var gameInfoList = BookmakerOddsHelper.BuildGameInfoList(
                gamesOnDate.Select(g => new GameRef(g.Id, g.HomeTeamId, g.AwayTeamId)),
                seasonTeams, dateGameDates);

            if (!gameInfoList.Any())
            {
                _logger.LogWarning("No game info could be built for date {Date}", gameDate.ToString("yyyy-MM-dd"));
                continue;
            }

            // Query at 6am Central on the game day — games are listed with pre-game odds
            // but no NHL games have started yet (earliest starts ~noon ET / 11am CT)
            var centralGameMorning = new DateTime(gameDate.Year, gameDate.Month, gameDate.Day, 6, 0, 0);
            var queryDate = TimeZoneInfo.ConvertTimeToUtc(centralGameMorning, centralZone);
            var responses = await GetOrFetchResponses(queryDate, gameDate);

            if (responses == null)
                continue;

            _logger.LogInformation("Date {Date}: {GameCount} DB games, {ResponseCount} API games",
                gameDate.ToString("yyyy-MM-dd"), gameInfoList.Count, responses.Count);

            await BookmakerOddsHelper.MapAndSave(_logger, _bookmakerOddsRepo, responses, gameInfoList, queryDate);
        }
    }

    private async Task<List<OddsApiResponse>?> GetOrFetchResponses(DateTime queryDate, DateTime gameDate)
    {
        var cached = await _bookmakerOddsRepo.GetCachedResponse(queryDate);

        if (cached != null)
        {
            _logger.LogInformation("Using cached response for {Date}", gameDate.ToString("yyyy-MM-dd"));

            var wrapper = JsonConvert.DeserializeObject<OddsApiHistoricalResponse>(cached.RawJson);
            return wrapper?.Data ?? new List<OddsApiResponse>();
        }

        _logger.LogInformation("Fetching historical odds for {Date}...", gameDate.ToString("yyyy-MM-dd"));
        var apiResult = await _oddsApiGetter.GetHistoricalOdds(queryDate);

        var remaining = _oddsApiGetter.GetRemainingRequests();
        if (remaining.HasValue)
            _logger.LogInformation("Odds API requests remaining: {Remaining}", remaining.Value);

        // Cache even empty responses so we don't re-fetch dates with no data
        if (!string.IsNullOrEmpty(apiResult.RawJson))
        {
            await _bookmakerOddsRepo.SaveRawResponse(apiResult.RawJson, queryDate);
            await _bookmakerOddsRepo.Commit();
        }

        if (!apiResult.Responses.Any())
        {
            _logger.LogWarning("No historical odds returned for {Date}", gameDate.ToString("yyyy-MM-dd"));
            return null;
        }

        return apiResult.Responses;
    }
}
