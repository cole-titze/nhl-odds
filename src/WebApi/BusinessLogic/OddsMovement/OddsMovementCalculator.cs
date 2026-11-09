using Entities.Models.Web;
using Entities.ServiceModels.Mappers;
using Entities.ViewModels;

namespace WebApi.BusinessLogic.OddsMovement;

/// <summary>
/// Builds the Game page's odds movement chart: the home team's win probability over time from the model, each
/// betting-partner widget, the sportsbooks' average (morning and close) and Kalshi (morning and close). Every price
/// has its margin removed (home / (home + away)) so it's comparable with the model. The chart covers game day only
/// (from midnight Eastern): the model predicts every upcoming game each morning, so its runs go back weeks, and on
/// that axis the market's few hours of prices were squeezed into the last pixels. A series with earlier points starts
/// at midnight with the last of them, so the model's level going into game day still shows.
/// </summary>
public static class OddsMovementCalculator
{
    public const string Model = "Model";
    public const string SportsbookAverage = "Sportsbooks' average";
    public const string Kalshi = "Kalshi";

    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    /// <summary>
    /// Where the chart starts: midnight Eastern on game day, or for an upcoming game with nothing from game day yet,
    /// midnight Eastern on the day of its latest price, so those show when they came in rather than all carried forward
    /// to a game day that hasn't happened
    /// </summary>
    public static DateTime ChartStartUtc(DateTime startUtc, IEnumerable<OddsSeriesVM> series)
    {
        var dayStart = GameDayStartUtc(startUtc);
        var latest = series.SelectMany(s => s.Points).Select(p => p.TimeUTC).DefaultIfEmpty(dayStart).Max();
        return latest < dayStart ? GameDayStartUtc(latest) : dayStart;
    }

    /// <summary>Midnight Eastern on the day of a time, in UTC</summary>
    public static DateTime GameDayStartUtc(DateTime startUtc)
    {
        var eastern = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), Eastern);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(eastern.Date, DateTimeKind.Unspecified), Eastern);
    }

    public static OddsMovementVM Calculate(OddsMovementGame game)
    {
        // Backfilled prices can be saved after the game; only what was known before it counts
        bool Before(DateTime time) => time <= game.StartUTC;
        var morningBooks = game.Morning.Where(p => Before(p.TimeUTC) && p.BookmakerName != Kalshi).ToList();
        var closingBooks = game.Closing.Where(p => Before(p.TimeUTC) && p.BookmakerName != Kalshi).ToList();
        var modelRuns = game.ModelRuns.Where(r => Before(r.TimeUTC)).OrderBy(r => r.TimeUTC).ToList();

        var series = new List<OddsSeriesVM>
        {
            Series(Model, "model", modelRuns.Select(r => Point(r.TimeUTC, r.HomeOdds))),
        };
        series.AddRange(game.Partner
            .Where(p => Before(p.TimeUTC))
            .GroupBy(p => p.Source)
            .OrderBy(g => g.Key)
            .Select(g => Series(g.Key, "partner", g.Select(p => Point(p.TimeUTC, NoVig(
                OddsApiResponseMapper.AmericanToImpliedProbability(p.HomePrice),
                OddsApiResponseMapper.AmericanToImpliedProbability(p.AwayPrice)))))));
        series.Add(Series(SportsbookAverage, "average", [AveragePoint(morningBooks), AveragePoint(closingBooks)]));
        series.Add(Series(Kalshi, "kalshi", [
            .. game.Morning.Where(p => Before(p.TimeUTC) && p.BookmakerName == Kalshi).Select(BookPoint),
            .. game.Closing.Where(p => Before(p.TimeUTC) && p.BookmakerName == Kalshi).Select(BookPoint),
        ]));

        return new OddsMovementVM
        {
            GameId = game.GameId,
            StartUTC = game.StartUTC,
            Series = series.Select(s => FromGameDay(s, ChartStartUtc(game.StartUTC, series))).Where(s => s.Points.Count > 0).ToList(),
            Move = Move(morningBooks, closingBooks, modelRuns.LastOrDefault()?.HomeOdds),
        };
    }

    public static double NoVig(double home, double away) => home / (home + away);

    /// <summary>
    /// Morning to close on the sportsbooks that priced both, so a book joining or leaving isn't read as a move
    /// </summary>
    private static MarketMoveVM? Move(List<BookPrice> morning, List<BookPrice> closing, double? model)
    {
        var closeByBook = closing.Where(Usable).GroupBy(p => p.BookmakerName).ToDictionary(g => g.Key, g => g.Last());
        var books = morning.Where(p => Usable(p) && closeByBook.ContainsKey(p.BookmakerName)).ToList();
        if (books.Count == 0)
            return null;
        var open = books.Average(p => NoVig(p.HomeOdds, p.AwayOdds));
        var close = books.Average(p => NoVig(closeByBook[p.BookmakerName].HomeOdds, closeByBook[p.BookmakerName].AwayOdds));
        return new MarketMoveVM
        {
            Open = open,
            Close = close,
            Model = model,
            TowardModel = model is { } m && Math.Abs(m - open) > 1e-9 ? (close - open) * Math.Sign(m - open) : null,
        };
    }

    private static bool Usable(BookPrice p) => p.HomeOdds > 0 && p.AwayOdds > 0;

    private static OddsPointVM? BookPoint(BookPrice p) => Usable(p) ? Point(p.TimeUTC, NoVig(p.HomeOdds, p.AwayOdds)) : null;

    private static OddsPointVM? AveragePoint(List<BookPrice> prices)
    {
        var usable = prices.Where(Usable).ToList();
        return usable.Count == 0
            ? null
            : Point(usable.Max(p => p.TimeUTC), usable.Average(p => NoVig(p.HomeOdds, p.AwayOdds)));
    }

    // Points before game day collapse into one at its start, holding the last of their values
    private static OddsSeriesVM FromGameDay(OddsSeriesVM series, DateTime dayStart)
    {
        var earlier = series.Points.LastOrDefault(p => p.TimeUTC < dayStart);
        var points = series.Points.Where(p => p.TimeUTC >= dayStart).ToList();
        if (earlier != null && (points.Count == 0 || points[0].TimeUTC > dayStart))
            points.Insert(0, Point(dayStart, earlier.HomeProbability));
        return new OddsSeriesVM { Name = series.Name, Kind = series.Kind, Points = points };
    }

    private static OddsPointVM Point(DateTime time, double home) => new() { TimeUTC = time, HomeProbability = home };

    // Points in time order, keeping only changes: a widget version that only changed another market adds nothing
    private static OddsSeriesVM Series(string name, string kind, IEnumerable<OddsPointVM?> points)
    {
        var kept = new List<OddsPointVM>();
        foreach (var point in points.OfType<OddsPointVM>().OrderBy(p => p.TimeUTC))
            if (kept.Count == 0 || Math.Abs(kept[^1].HomeProbability - point.HomeProbability) > 1e-9)
                kept.Add(point);
        return new OddsSeriesVM { Name = name, Kind = kind, Points = kept };
    }
}
