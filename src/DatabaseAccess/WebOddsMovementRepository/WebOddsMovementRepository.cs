using Entities.Models.Web;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebOddsMovementRepository;

public class WebOddsMovementRepository(GameDbContext dbContext) : IWebOddsMovementRepository
{
    private const string TwoWayMoneyline = "MONEY_LINE_2_WAY";

    public async Task<OddsMovementGame?> GetOddsMovement(int gameId)
    {
        var start = await dbContext.GameRaw.AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => (DateTime?)g.GameDateUTC)
            .FirstOrDefaultAsync();
        if (start is not { } startUtc)
            return null;

        var modelRuns = await dbContext.GameOdds.AsNoTracking()
            .Where(o => o.GameId == gameId && o.RunDateUTC < startUtc)
            .Select(o => new ModelRun(o.RunDateUTC, o.HomeOdds))
            .ToListAsync();
        var morning = await dbContext.BookmakerOdds.AsNoTracking()
            .Where(o => o.GameId == gameId)
            .Select(o => new BookPrice(o.BookmakerName, o.FetchedDateUTC, o.HomeOdds, o.AwayOdds))
            .ToListAsync();
        var closing = await dbContext.BookmakerClosingOdds.AsNoTracking()
            .Where(o => o.GameId == gameId)
            .Select(o => new BookPrice(o.BookmakerName, o.PriceTimeUTC, o.HomeOdds, o.AwayOdds))
            .ToListAsync();
        // After puck drop the widget shows live odds, so only versions from before the start
        var partnerRows = await dbContext.PartnerOdds.AsNoTracking()
            .Where(o => o.GameId == gameId && o.Market == TwoWayMoneyline && o.PartnerUpdatedUTC < o.StartTimeUTC)
            .Select(o => new { o.PartnerName, o.Country, o.PartnerUpdatedUTC, o.IsHome, o.Price })
            .ToListAsync();

        var partner = partnerRows
            .GroupBy(o => (o.PartnerName, o.Country, o.PartnerUpdatedUTC))
            .Where(v => v.Any(o => o.IsHome) && v.Any(o => !o.IsHome))
            .Select(v => new PartnerPrice($"{v.Key.PartnerName} ({v.Key.Country})", Utc(v.Key.PartnerUpdatedUTC),
                (int)v.First(o => o.IsHome).Price, (int)v.First(o => !o.IsHome).Price))
            .ToList();

        return new OddsMovementGame
        {
            GameId = gameId,
            StartUTC = Utc(startUtc),
            ModelRuns = modelRuns.Select(r => r with { TimeUTC = Utc(r.TimeUTC) }).ToList(),
            Morning = morning.Select(p => p with { TimeUTC = Utc(p.TimeUTC) }).ToList(),
            Closing = closing.Select(p => p with { TimeUTC = Utc(p.TimeUTC) }).ToList(),
            Partner = partner,
        };
    }

    // timestamp columns come back unspecified; mark them UTC so the JSON says so
    private static DateTime Utc(DateTime time) => DateTime.SpecifyKind(time, DateTimeKind.Utc);
}
