using System.Text.Json;
using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using Entities.ServiceModels.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Kalshi;
using Services.OddsApi;

namespace BookmakerOddsGetter;

public class BookmakerOddsFetcher
{
    private readonly NhlDbContext _dbContext;
    private readonly IBookmakerOddsRepository _bookmakerOddsRepo;
    private readonly IOddsApiGetter _oddsApiGetter;
    private readonly IKalshiGetter _kalshiGetter;
    private readonly ILogger<BookmakerOddsFetcher> _logger;

    public BookmakerOddsFetcher(
        NhlDbContext dbContext,
        IBookmakerOddsRepository bookmakerOddsRepo,
        IOddsApiGetter oddsApiGetter,
        ILoggerFactory loggerFactory,
        IKalshiGetter kalshiGetter)
    {
        _dbContext = dbContext;
        _bookmakerOddsRepo = bookmakerOddsRepo;
        _oddsApiGetter = oddsApiGetter;
        _kalshiGetter = kalshiGetter;
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

        // Fetch from Kalshi
        await FetchAndSaveKalshiOdds(gameInfoList);
    }

    private async Task CacheResponseByGameDate(string rawJson, IEnumerable<DateTime> gameDatesUtc)
    {
        var centralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

        // Wrap the upcoming odds array in the historical response format so the
        // backfiller can deserialize it with the same OddsApiHistoricalResponse type
        var wrappedJson = JsonSerializer.Serialize(new { data = JsonSerializer.Deserialize<JsonElement>(rawJson) });

        var distinctDates = gameDatesUtc
            .Select(d => TimeZoneInfo.ConvertTimeFromUtc(d, centralZone).Date)
            .Distinct();

        foreach (var gameDate in distinctDates)
        {
            var centralMorning = new DateTime(gameDate.Year, gameDate.Month, gameDate.Day, 6, 0, 0);
            var queryDate = TimeZoneInfo.ConvertTimeToUtc(centralMorning, centralZone);

            var existing = await _bookmakerOddsRepo.GetCachedResponse(queryDate);
            if (existing != null)
                continue;

            await _bookmakerOddsRepo.SaveRawResponse(wrappedJson, queryDate);
            _logger.LogInformation("Cached upcoming odds response for game date {Date}", gameDate.ToString("yyyy-MM-dd"));
        }

        await _bookmakerOddsRepo.Commit();
    }

    private async Task FetchAndSaveKalshiOdds(List<OddsApiResponseMapper.GameInfo> gameInfoList)
    {
        _logger.LogInformation("Fetching Kalshi odds...");

        var gameMarkets = await _kalshiGetter.GetOpenMarkets("KXNHLGAME");
        var spreadMarkets = await _kalshiGetter.GetOpenMarkets("KXNHLSPREAD");
        var totalMarkets = await _kalshiGetter.GetOpenMarkets("KXNHLTOTAL");

        var mapped = KalshiResponseMapper.Map(gameMarkets, spreadMarkets, totalMarkets, gameInfoList);

        if (!mapped.H2H.Any() && !mapped.Spreads.Any() && !mapped.Totals.Any())
        {
            _logger.LogWarning("No Kalshi odds could be matched to games.");
            return;
        }

        // Deduplicate by composite key
        var h2h = mapped.H2H
            .GroupBy(x => (x.GameId, x.BookmakerName))
            .Select(g => g.First())
            .ToList();
        var spreads = mapped.Spreads
            .GroupBy(x => (x.GameId, x.BookmakerName))
            .Select(g => g.First())
            .ToList();
        var totals = mapped.Totals
            .GroupBy(x => (x.GameId, x.BookmakerName))
            .Select(g => g.First())
            .ToList();

        if (h2h.Any())
            await _bookmakerOddsRepo.AddOrUpdateBookmakerOdds(h2h);
        if (spreads.Any())
            await _bookmakerOddsRepo.AddOrUpdateBookmakerSpreads(spreads);
        if (totals.Any())
            await _bookmakerOddsRepo.AddOrUpdateBookmakerTotals(totals);
        await _bookmakerOddsRepo.Commit();

        _logger.LogInformation("Saved Kalshi: {H2H} h2h, {Spreads} spreads, {Totals} totals",
            h2h.Count, spreads.Count, totals.Count);
    }
}
