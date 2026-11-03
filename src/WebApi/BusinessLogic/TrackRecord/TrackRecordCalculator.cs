using Entities.Models.Web;
using Entities.Types;
using Entities.ViewModels;

namespace WebApi.BusinessLogic.TrackRecord;

/// <summary>
/// Turns a season's played games into the Track Record page's numbers. Bookmaker prices are compared
/// with their margin removed (home / (home + away)), so they're fair probabilities like the model's.
/// </summary>
public static class TrackRecordCalculator
{
    public const double BinWidth = 0.05;
    public const string MorningAverage = "Bookmakers' average (morning)";
    public const string ClosingAverage = "Bookmakers' average (closing)";
    private const double Epsilon = 1e-15;
    private static readonly TimeZoneInfo Central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    public static TrackRecordVM Calculate(int seasonStartYear, bool liveOnly, IReadOnlyList<TrackRecordGame> allGames)
    {
        var games = liveOnly ? allGames.Where(g => g.IsLive).ToList() : allGames.ToList();
        return new TrackRecordVM
        {
            SeasonStartYear = seasonStartYear,
            LiveOnly = liveOnly,
            GameCount = games.Count,
            LiveGameCount = games.Count(g => g.IsLive),
            ModelLogLoss = games.Count == 0 ? null : games.Average(g => LogLoss(g.ModelHomeOdds, g.Winner)),
            ModelAccuracy = games.Count == 0 ? null : games.Average(g => Correct(g.ModelHomeOdds, g.Winner) ? 1.0 : 0.0),
            Calibration = Calibrate(games),
            LogLossByDate = LogLossByDate(games),
            Sources = Sources(games),
            ClosingLine = ClosingLine(games),
        };
    }

    /// <summary>A bookmaker's home win probability without its margin, or null for an unusable price</summary>
    public static double? NoVig(TrackRecordPrice price) =>
        price.HomeOdds > 0 && price.AwayOdds > 0 ? price.HomeOdds / (price.HomeOdds + price.AwayOdds) : null;

    /// <summary>Average no-vig home probability across the given prices, or null if none are usable</summary>
    public static double? Average(IEnumerable<TrackRecordPrice> prices)
    {
        var usable = prices.Select(NoVig).OfType<double>().ToList();
        return usable.Count == 0 ? null : usable.Average();
    }

    public static double LogLoss(double homeProbability, Winner winner)
    {
        var p = winner == Winner.HOME ? homeProbability : 1 - homeProbability;
        return -Math.Log(Math.Clamp(p, Epsilon, 1 - Epsilon));
    }

    // Same rule as the predictor: 50% counts as picking the home team
    public static bool Correct(double homeProbability, Winner winner) => homeProbability >= 0.5 == (winner == Winner.HOME);

    private static List<CalibrationBinVM> Calibrate(List<TrackRecordGame> games)
    {
        var binCount = (int)Math.Round(1 / BinWidth);
        return games
            .GroupBy(g => Math.Min((int)(g.ModelHomeOdds / BinWidth), binCount - 1))
            .OrderBy(b => b.Key)
            .Select(b => new CalibrationBinVM
            {
                Low = Math.Round(b.Key * BinWidth, 4),
                High = Math.Round((b.Key + 1) * BinWidth, 4),
                Games = b.Count(),
                MeanPredicted = b.Average(g => g.ModelHomeOdds),
                HomeWinRate = b.Average(g => g.Winner == Winner.HOME ? 1.0 : 0.0),
            })
            .ToList();
    }

    private static List<LogLossPointVM> LogLossByDate(List<TrackRecordGame> games)
    {
        var points = new List<LogLossPointVM>();
        double modelSum = 0, marketSum = 0;
        int modelCount = 0, marketCount = 0;
        foreach (var day in games.GroupBy(g => TimeZoneInfo.ConvertTimeFromUtc(g.GameDateUTC, Central).Date).OrderBy(d => d.Key))
        {
            foreach (var game in day)
            {
                modelSum += LogLoss(game.ModelHomeOdds, game.Winner);
                modelCount++;
                if (Average(game.Morning) is { } market)
                {
                    marketSum += LogLoss(market, game.Winner);
                    marketCount++;
                }
            }
            points.Add(new LogLossPointVM
            {
                Date = day.Key,
                Games = modelCount,
                Model = modelSum / modelCount,
                Market = marketCount == 0 ? null : marketSum / marketCount,
            });
        }
        return points;
    }

    private static List<SourceComparisonVM> Sources(List<TrackRecordGame> games)
    {
        var sources = new List<SourceComparisonVM>();
        Add(MorningAverage, g => Average(g.Morning));
        Add(ClosingAverage, g => Average(g.Closing));
        var books = games.SelectMany(g => g.Morning.Select(p => p.BookmakerName)).Distinct()
            .Select(name => (Name: name, Count: games.Count(g => g.Morning.Any(p => p.BookmakerName == name && NoVig(p) != null))))
            .OrderByDescending(b => b.Count).ThenBy(b => b.Name);
        foreach (var (name, _) in books)
            Add(name, g => g.Morning.FirstOrDefault(p => p.BookmakerName == name) is { } p ? NoVig(p) : null);
        return sources;

        // The model is scored on the same games as the source, so the two are compared fairly
        void Add(string name, Func<TrackRecordGame, double?> probability)
        {
            var paired = games.Select(g => (Game: g, Source: probability(g)))
                .Where(x => x.Source != null)
                .Select(x => (x.Game, Source: x.Source!.Value))
                .ToList();
            if (paired.Count == 0)
                return;
            sources.Add(new SourceComparisonVM
            {
                Name = name,
                Games = paired.Count,
                LogLoss = paired.Average(x => LogLoss(x.Source, x.Game.Winner)),
                Accuracy = paired.Average(x => Correct(x.Source, x.Game.Winner) ? 1.0 : 0.0),
                ModelLogLoss = paired.Average(x => LogLoss(x.Game.ModelHomeOdds, x.Game.Winner)),
                ModelAccuracy = paired.Average(x => Correct(x.Game.ModelHomeOdds, x.Game.Winner) ? 1.0 : 0.0),
            });
        }
    }

    private static ClosingLineVM? ClosingLine(List<TrackRecordGame> games)
    {
        var result = new ClosingLineVM();
        double moveSum = 0;
        foreach (var game in games)
        {
            // Only bookmakers with both prices, so a change in which books priced the game isn't read as a move
            var closingByBook = game.Closing.Where(p => NoVig(p) != null).ToDictionary(p => p.BookmakerName);
            var books = game.Morning.Where(p => NoVig(p) != null && closingByBook.ContainsKey(p.BookmakerName)).ToList();
            if (books.Count == 0)
                continue;
            var open = books.Average(p => NoVig(p)!.Value);
            var close = books.Average(p => NoVig(closingByBook[p.BookmakerName])!.Value);
            var model = game.ModelHomeOdds;

            result.Games++;
            if (Math.Abs(model - close) < Math.Abs(open - close))
                result.ModelCloser++;
            var move = (close - open) * Math.Sign(model - open);
            moveSum += move;
            if (move > 1e-9)
                result.MovedTowardModel++;
            else if (move < -1e-9)
                result.MovedAwayFromModel++;
        }
        if (result.Games == 0)
            return null;
        result.AverageMoveTowardModel = moveSum / result.Games;
        return result;
    }
}
