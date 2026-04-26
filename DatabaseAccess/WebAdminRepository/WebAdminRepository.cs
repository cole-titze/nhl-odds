using DatabaseAccess.WebAdminRepository.Mappers;
using Entities.Models.Web;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebAdminRepository;

public class WebAdminRepository : IWebAdminRepository
{
    private readonly GameDbContext _db;

    public WebAdminRepository(GameDbContext db)
    {
        _db = db;
    }

    public async Task<List<ErrorLog>> GetErrorLogs(int? seasonStartYear)
    {
        var query = _db.ErrorLog.AsQueryable();

        if (seasonStartYear.HasValue)
            query = query.Where(e => e.SeasonStartYear == seasonStartYear.Value);

        var dbErrors = await query
            .OrderByDescending(e => e.TimestampUTC)
            .Take(50)
            .ToListAsync();

        return dbErrors.Select(DbErrorLogToErrorLogMapper.Map).ToList();
    }

    public async Task<List<SeasonHealthCheck>> GetHealthChecks()
    {
        var games = await _db.GameRaw
            .Select(g => new { g.Id, g.SeasonStartYear, g.HasBeenPlayed, g.GameDateUTC })
            .ToListAsync();

        var gameOddsGameIds = new HashSet<int>(
            await _db.GameOdds.Select(go => go.GameId).Distinct().ToListAsync());
        var spreadGameIds = new HashSet<int>(
            await _db.GameSpreadTotalOdds.Where(st => st.ModelId == 2).Select(st => st.GameId).Distinct().ToListAsync());
        var totalGameIds = new HashSet<int>(
            await _db.GameSpreadTotalOdds.Where(st => st.ModelId == 3).Select(st => st.GameId).Distinct().ToListAsync());
        var liveBookmakerOddsGameIds = new HashSet<int>(
            await _db.BookmakerOdds
                .Join(_db.GameRaw, bo => bo.GameId, g => g.Id, (bo, g) => new { bo.GameId, bo.MarketLastUpdate, g.GameDateUTC })
                .Where(x => x.MarketLastUpdate > x.GameDateUTC)
                .Select(x => x.GameId)
                .Distinct()
                .ToListAsync());
        var bookmakerGameIds = new HashSet<int>(
            await _db.BookmakerOdds.Select(bo => bo.GameId).Distinct().ToListAsync());
        var spreadBookmakerGameIds = new HashSet<int>(
            await _db.BookmakerSpreads.Select(bs => bs.GameId).Distinct().ToListAsync());
        var totalBookmakerGameIds = new HashSet<int>(
            await _db.BookmakerTotals.Select(bt => bt.GameId).Distinct().ToListAsync());
        var kalshiGameIds = new HashSet<int>(
            await _db.BookmakerOdds.Where(bo => bo.BookmakerKey == "kalshi").Select(bo => bo.GameId).Distinct().ToListAsync());
        var kalshiSpreadGameIds = new HashSet<int>(
            await _db.BookmakerSpreads.Where(bs => bs.BookmakerKey == "kalshi").Select(bs => bs.GameId).Distinct().ToListAsync());
        var kalshiTotalGameIds = new HashSet<int>(
            await _db.BookmakerTotals.Where(bt => bt.BookmakerKey == "kalshi").Select(bt => bt.GameId).Distinct().ToListAsync());
        var cleanedGameIds = new HashSet<int>(
            await _db.GameCleaned.Select(gc => gc.GameId).Distinct().ToListAsync());

        var centralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var oddsFetchDateRaws = await _db.BookmakerOddsResponse
            .Where(r => r.QueryDateUTC != null)
            .Select(r => r.QueryDateUTC!.Value)
            .ToListAsync();
        var oddsFetchDateSet = new HashSet<DateTime>(
            oddsFetchDateRaws.Select(d => TimeZoneInfo.ConvertTimeFromUtc(d, centralZone).Date));

        var errorCounts = await _db.ErrorLog
            .GroupBy(e => e.SeasonStartYear)
            .Select(g => new { SeasonStartYear = g.Key, Count = g.Count() })
            .ToListAsync();
        var errorCountDict = errorCounts
            .Where(e => e.SeasonStartYear.HasValue)
            .ToDictionary(e => e.SeasonStartYear!.Value, e => e.Count);

        var checks = games
            .GroupBy(g => g.SeasonStartYear)
            .Select(g =>
            {
                var today = DateTime.UtcNow.Date;
                var allGames = g.ToList();
                var playedBeforeToday = allGames.Where(x => x.HasBeenPlayed && x.GameDateUTC.Date < today).ToList();
                var playedThroughToday = allGames.Where(x => x.HasBeenPlayed && x.GameDateUTC.Date <= today).ToList();
                var gameDates = playedBeforeToday
                    .Select(x => TimeZoneInfo.ConvertTimeFromUtc(x.GameDateUTC, centralZone).Date)
                    .Distinct().ToList();
                return new SeasonHealthCheck
                {
                    SeasonStartYear = g.Key,
                    TotalGames = allGames.Count,
                    PlayedGames = playedThroughToday.Count,
                    MissingPredictions = g.Key <= 2009 ? -1
                        : allGames.Count(x => !gameOddsGameIds.Contains(x.Id))
                        + allGames.Count(x => !spreadGameIds.Contains(x.Id))
                        + allGames.Count(x => !totalGameIds.Contains(x.Id)),
                    MissingBookmakerOdds = g.Key < 2020 ? -1
                        : playedThroughToday.Count(x => !bookmakerGameIds.Contains(x.Id))
                        + playedThroughToday.Count(x => !spreadBookmakerGameIds.Contains(x.Id))
                        + playedThroughToday.Count(x => !totalBookmakerGameIds.Contains(x.Id)),
                    MissingGameCleaned = allGames.Count(x => !cleanedGameIds.Contains(x.Id)),
                    MissingOddsFetchDays = g.Key < 2020 ? -1
                        : gameDates.Count(d => !oddsFetchDateSet.Contains(d)),
                    LiveBookmakerOdds = allGames.Count(x => liveBookmakerOddsGameIds.Contains(x.Id)),
                    MissingKalshiOdds = g.Key < 2025 ? -1
                        : playedThroughToday.Count(x => !kalshiGameIds.Contains(x.Id))
                        + playedThroughToday.Count(x => !kalshiSpreadGameIds.Contains(x.Id))
                        + playedThroughToday.Count(x => !kalshiTotalGameIds.Contains(x.Id)),
                    ErrorCount = errorCountDict.GetValueOrDefault(g.Key, 0),
                };
            })
            .OrderByDescending(c => c.SeasonStartYear)
            .ToList();

        return checks;
    }
}