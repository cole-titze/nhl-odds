using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using Entities.ServiceModels.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Kalshi;

namespace BookmakerOddsGetter;

public class KalshiOddsFetcher
{
    private readonly NhlDbContext _dbContext;
    private readonly IBookmakerOddsRepository _bookmakerOddsRepo;
    private readonly IKalshiGetter _kalshiGetter;
    private readonly ILogger<KalshiOddsFetcher> _logger;

    public KalshiOddsFetcher(
        NhlDbContext dbContext,
        IBookmakerOddsRepository bookmakerOddsRepo,
        IKalshiGetter kalshiGetter,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _bookmakerOddsRepo = bookmakerOddsRepo;
        _kalshiGetter = kalshiGetter;
        _logger = loggerFactory.CreateLogger<KalshiOddsFetcher>();
    }

    public async Task FetchAndSaveKalshiOdds()
    {
        var unplayedGames = await _dbContext.GameRaw
            .Where(g => !g.HasBeenPlayed)
            .Select(g => new { g.Id, g.HomeTeamId, g.AwayTeamId, g.SeasonStartYear })
            .ToListAsync();

        if (!unplayedGames.Any())
        {
            _logger.LogInformation("No unplayed games found. Skipping Kalshi fetch.");
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

        _logger.LogInformation("Fetching Kalshi odds for {Count} games...", unplayedGames.Count);

        var gameMarkets = await _kalshiGetter.GetOpenMarkets("KXNHLGAME");
        var spreadMarkets = await _kalshiGetter.GetOpenMarkets("KXNHLSPREAD");
        var totalMarkets = await _kalshiGetter.GetOpenMarkets("KXNHLTOTAL");

        var mapped = KalshiResponseMapper.Map(gameMarkets, spreadMarkets, totalMarkets, gameInfoList);

        if (!mapped.H2H.Any() && !mapped.Spreads.Any() && !mapped.Totals.Any())
        {
            _logger.LogWarning("No Kalshi odds could be matched to games.");
            return;
        }

        var h2h = mapped.H2H.GroupBy(x => (x.GameId, x.BookmakerName)).Select(g => g.First()).ToList();
        var spreads = mapped.Spreads.GroupBy(x => (x.GameId, x.BookmakerName)).Select(g => g.First()).ToList();
        var totals = mapped.Totals.GroupBy(x => (x.GameId, x.BookmakerName)).Select(g => g.First()).ToList();

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