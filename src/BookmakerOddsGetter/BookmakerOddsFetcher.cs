using System.Text.Json;
using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using Entities.ServiceModels.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.OddsApi;

namespace BookmakerOddsGetter;

public class BookmakerOddsFetcher
{
    private readonly NhlDbContext _dbContext;
    private readonly IBookmakerOddsRepository _bookmakerOddsRepo;
    private readonly IOddsApiGetter _oddsApiGetter;
    private readonly ILogger<BookmakerOddsFetcher> _logger;

    public BookmakerOddsFetcher(
        NhlDbContext dbContext,
        IBookmakerOddsRepository bookmakerOddsRepo,
        IOddsApiGetter oddsApiGetter,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _bookmakerOddsRepo = bookmakerOddsRepo;
        _oddsApiGetter = oddsApiGetter;
        _logger = loggerFactory.CreateLogger<BookmakerOddsFetcher>();
    }

    public async Task FetchAndSaveBookmakerOdds()
    {
        var unplayedGames = await _dbContext.GameRaw
            .Where(g => !g.HasBeenPlayed)
            .Select(g => new { g.Id, g.HomeTeamId, g.AwayTeamId, g.SeasonStartYear })
            .ToListAsync();

        if (!unplayedGames.Any())
        {
            _logger.LogInformation("No unplayed games found. Skipping bookmaker odds fetch.");
            return;
        }

        var seasonStartYear = unplayedGames.First().SeasonStartYear;
        var seasonTeams = await _dbContext.SeasonTeam
            .Where(t => t.SeasonStartYear == seasonStartYear)
            .ToDictionaryAsync(t => t.TeamId, t => t.Name);

        var gameIds = unplayedGames.Select(g => g.Id).ToHashSet();
        var gameDates = await _dbContext.GameRaw
            .Where(g => gameIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.GameDateUTC);

        var gameInfoList = BookmakerOddsHelper.BuildGameInfoList(
            unplayedGames.Select(g => new GameRef(g.Id, g.HomeTeamId, g.AwayTeamId)), seasonTeams, gameDates);

        // Fetch from The Odds API
        _logger.LogInformation("Fetching bookmaker odds for {Count} games...", unplayedGames.Count);
        var apiResult = await _oddsApiGetter.GetUpcomingOdds();

        if (!apiResult.Responses.Any())
        {
            _logger.LogWarning("No odds returned from The Odds API.");
        }
        else
        {
            await _bookmakerOddsRepo.SaveRawResponse(apiResult.RawJson);
            await CacheResponseByGameDate(apiResult.RawJson, gameDates.Values);

            BookmakerOddsHelper.LogMatchingDetails(_logger, apiResult.Responses, gameInfoList);
            await BookmakerOddsHelper.MapAndSave(_logger, _bookmakerOddsRepo, apiResult.Responses, gameInfoList);

            var remaining = _oddsApiGetter.GetRemainingRequests();
            if (remaining.HasValue)
                _logger.LogInformation("Odds API requests remaining: {Remaining}", remaining.Value);
        }

    }

    private async Task CacheResponseByGameDate(string rawJson, IEnumerable<DateTime> gameDatesUtc)
    {
        var centralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

        // Wrap the upcoming odds array in the historical response format so the
        // backfiller can deserialize it with the same OddsApiHistoricalResponse type
        var wrappedJson = JsonSerializer.Serialize(new { data = JsonSerializer.Deserialize<JsonElement>(rawJson) });

        // Only cache today's games: a response fetched days earlier isn't that day's morning odds
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, centralZone).Date;
        if (!gameDatesUtc.Any(d => TimeZoneInfo.ConvertTimeFromUtc(d, centralZone).Date == today))
            return;

        var centralMorning = new DateTime(today.Year, today.Month, today.Day, 6, 0, 0);
        var queryDate = TimeZoneInfo.ConvertTimeToUtc(centralMorning, centralZone);
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(today, centralZone);

        // First fetch of the day wins; an older row (e.g. from the old cache-ahead behavior)
        // is shadowed because GetCachedResponse returns the newest
        var existing = await _bookmakerOddsRepo.GetCachedResponse(queryDate);
        if (existing != null && existing.FetchedDateUTC >= todayStartUtc)
            return;

        await _bookmakerOddsRepo.SaveRawResponse(wrappedJson, queryDate);
        _logger.LogInformation("Cached upcoming odds response for game date {Date}", today.ToString("yyyy-MM-dd"));

        await _bookmakerOddsRepo.Commit();
    }
}