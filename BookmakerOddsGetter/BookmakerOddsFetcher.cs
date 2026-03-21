using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
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

        _logger.LogInformation("Fetching bookmaker odds for {Count} games...", unplayedGames.Count);
        var apiResult = await _oddsApiGetter.GetUpcomingOdds();

        if (!apiResult.Responses.Any())
        {
            _logger.LogWarning("No odds returned from The Odds API.");
            return;
        }

        await _bookmakerOddsRepo.SaveRawResponse(apiResult.RawJson);

        BookmakerOddsHelper.LogMatchingDetails(_logger, apiResult.Responses, gameInfoList);
        await BookmakerOddsHelper.MapAndSave(_logger, _bookmakerOddsRepo, apiResult.Responses, gameInfoList);

        var remaining = _oddsApiGetter.GetRemainingRequests();
        if (remaining.HasValue)
            _logger.LogInformation("Odds API requests remaining: {Remaining}", remaining.Value);
    }
}
