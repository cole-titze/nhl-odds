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
        // Get unplayed games
        var unplayedGames = await _dbContext.GameRaw
            .Where(g => !g.HasBeenPlayed)
            .Select(g => new { g.Id, g.HomeTeamId, g.SeasonStartYear })
            .ToListAsync();

        if (!unplayedGames.Any())
        {
            _logger.LogInformation("No unplayed games found. Skipping bookmaker odds fetch.");
            return;
        }

        // Check which games already have bookmaker odds
        var existingGameIds = await _bookmakerOddsRepo.GetGameIdsWithBookmakerOdds();
        var gamesNeedingOdds = unplayedGames.Where(g => !existingGameIds.Contains(g.Id)).ToList();

        if (!gamesNeedingOdds.Any())
        {
            _logger.LogInformation("All unplayed games already have bookmaker odds. Skipping API call.");
            return;
        }

        // Build gameId -> home team name map using SeasonTeam
        var seasonStartYear = gamesNeedingOdds.First().SeasonStartYear;
        var seasonTeams = await _dbContext.SeasonTeam
            .Where(t => t.SeasonStartYear == seasonStartYear)
            .ToDictionaryAsync(t => t.TeamId, t => t.Name);

        // Also fetch game dates for matching
        var gameIds = gamesNeedingOdds.Select(g => g.Id).ToHashSet();
        var gameDates = await _dbContext.GameRaw
            .Where(g => gameIds.Contains(g.Id))
            .Select(g => new { g.Id, g.HomeTeamId, g.GameDateUTC })
            .ToDictionaryAsync(g => g.Id);

        var gameInfoList = new List<OddsApiResponseMapper.GameInfo>();
        foreach (var game in gamesNeedingOdds)
        {
            if (seasonTeams.TryGetValue(game.HomeTeamId, out var teamName) && gameDates.TryGetValue(game.Id, out var dateInfo))
            {
                gameInfoList.Add(new OddsApiResponseMapper.GameInfo
                {
                    GameId = game.Id,
                    HomeTeamName = teamName,
                    GameDateUTC = dateInfo.GameDateUTC,
                });
            }
        }

        // Call The Odds API (single request)
        _logger.LogInformation("Fetching bookmaker odds for {Count} games...", gamesNeedingOdds.Count);
        var apiResult = await _oddsApiGetter.GetUpcomingOdds();

        if (!apiResult.Responses.Any())
        {
            _logger.LogWarning("No odds returned from The Odds API.");
            return;
        }

        // Save raw response to DB for recovery
        await _bookmakerOddsRepo.SaveRawResponse(apiResult.RawJson);

        // Log matching details
        _logger.LogInformation("API returned {Count} games. DB has {DbCount} unplayed games needing odds.",
            apiResult.Responses.Count, gameInfoList.Count);
        foreach (var r in apiResult.Responses)
        {
            var apiDateUtc = r.CommenceTime.ToUniversalTime().Date;
            var candidates = gameInfoList.Where(g => g.GameDateUTC.Date == apiDateUtc).ToList();
            var bestCandidate = candidates
                .Select(g => new { g.GameId, g.HomeTeamName, Score = FuzzySharp.Fuzz.TokenSortRatio(g.HomeTeamName.ToLower(), r.HomeTeam.ToLower()) })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            _logger.LogInformation("API: {Home} vs {Away} | date={UtcDate} | candidates={Count} | best={BestName} (id={BestId}, score={Score})",
                r.HomeTeam, r.AwayTeam, apiDateUtc, candidates.Count,
                bestCandidate?.HomeTeamName ?? "none", bestCandidate?.GameId ?? 0, bestCandidate?.Score ?? 0);
        }

        // Map API responses to DbBookmakerOdds
        var bookmakerOdds = OddsApiResponseMapper.Map(apiResult.Responses, gameInfoList);

        if (!bookmakerOdds.Any())
        {
            _logger.LogWarning("No bookmaker odds could be matched to games.");
            return;
        }

        // Save
        await _bookmakerOddsRepo.AddBookmakerOdds(bookmakerOdds);
        await _bookmakerOddsRepo.Commit();

        _logger.LogInformation("Saved {Count} bookmaker odds records.", bookmakerOdds.Count);

        var remaining = _oddsApiGetter.GetRemainingRequests();
        if (remaining.HasValue)
            _logger.LogInformation("Odds API requests remaining: {Remaining}", remaining.Value);
    }

}
