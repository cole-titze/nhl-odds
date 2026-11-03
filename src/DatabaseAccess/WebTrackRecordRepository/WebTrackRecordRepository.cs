using Entities.Models.Web;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebTrackRecordRepository;

public class WebTrackRecordRepository(GameDbContext dbContext) : IWebTrackRecordRepository
{
    public async Task<List<TrackRecordGame>> GetPlayedGames(int seasonStartYear)
    {
        // Only the columns needed: GameRaw also holds large summary text
        var games = await dbContext.GameRaw
            .AsNoTracking()
            .Where(g => g.SeasonStartYear == seasonStartYear && g.HasBeenPlayed)
            .Select(g => new { g.Id, g.GameDateUTC, g.Winner })
            .ToListAsync();

        var predictions = await (
            from o in dbContext.GameOdds.AsNoTracking()
            join g in dbContext.GameRaw on o.GameId equals g.Id
            where g.SeasonStartYear == seasonStartYear && g.HasBeenPlayed
            select new { o.GameId, o.RunDateUTC, o.HomeOdds, g.GameDateUTC }).ToListAsync();

        var morning = await (
            from o in dbContext.BookmakerOdds.AsNoTracking()
            join g in dbContext.GameRaw on o.GameId equals g.Id
            where g.SeasonStartYear == seasonStartYear && g.HasBeenPlayed
            select new { o.GameId, o.BookmakerName, o.HomeOdds, o.AwayOdds }).ToListAsync();

        var closing = await (
            from o in dbContext.BookmakerClosingOdds.AsNoTracking()
            join g in dbContext.GameRaw on o.GameId equals g.Id
            where g.SeasonStartYear == seasonStartYear && g.HasBeenPlayed
            select new { o.GameId, o.BookmakerName, o.HomeOdds, o.AwayOdds }).ToListAsync();

        // The newest prediction saved before puck drop, else the newest at all (a walk-forward backfill)
        var predictionByGame = predictions
            .GroupBy(p => p.GameId)
            .ToDictionary(g => g.Key, g =>
            {
                var live = g.Where(p => p.RunDateUTC < p.GameDateUTC).MaxBy(p => p.RunDateUTC);
                var chosen = live ?? g.MaxBy(p => p.RunDateUTC)!;
                return (chosen.HomeOdds, IsLive: live != null);
            });
        var morningByGame = morning.ToLookup(o => o.GameId, o => new TrackRecordPrice(o.BookmakerName, o.HomeOdds, o.AwayOdds));
        var closingByGame = closing.ToLookup(o => o.GameId, o => new TrackRecordPrice(o.BookmakerName, o.HomeOdds, o.AwayOdds));

        return games
            .Where(g => predictionByGame.ContainsKey(g.Id))
            .OrderBy(g => g.GameDateUTC)
            .Select(g => new TrackRecordGame
            {
                GameId = g.Id,
                GameDateUTC = g.GameDateUTC,
                Winner = g.Winner,
                ModelHomeOdds = predictionByGame[g.Id].HomeOdds,
                IsLive = predictionByGame[g.Id].IsLive,
                Morning = morningByGame[g.Id].ToList(),
                Closing = closingByGame[g.Id].ToList(),
            })
            .ToList();
    }
}
