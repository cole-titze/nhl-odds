using Entities.DbModels;
using Entities.ServiceModels.Kalshi;
using FuzzySharp;

namespace Entities.ServiceModels.Mappers;

public static class KalshiResponseMapper
{
    private const string BOOKMAKER_NAME = "Kalshi";
    private const string BOOKMAKER_KEY = "kalshi";
    private const double STANDARD_PUCK_LINE = 1.5;
    private const double DEFAULT_TOTAL_LINE = 5.5;
    private static readonly TimeZoneInfo CentralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    public static DateTime ToCentralDate(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, CentralZone).Date;

    /// <summary>
    /// Kalshi subtitles use abbreviated city names ("WSH Capitals", "TB Lightning").
    /// Strip the abbreviation to get just the mascot name for better fuzzy matching
    /// against full DB names like "Washington Capitals".
    /// </summary>
    private static string StripAbbreviation(string kalshiName)
    {
        var spaceIdx = kalshiName.IndexOf(' ');
        return spaceIdx >= 0 ? kalshiName[(spaceIdx + 1)..] : kalshiName;
    }

    /// <summary>
    /// Score a Kalshi team name against a DB team name, trying both the raw name
    /// and the abbreviation-stripped name to handle both "Boston Bruins" and "WSH Capitals".
    /// </summary>
    /// <summary>
    /// Extract the team name from a spread subtitle like "Washington wins by over 1.5 goals".
    /// Returns the part before " wins by over".
    /// </summary>
    public static string ExtractSpreadTeamName(string subtitle)
    {
        var idx = subtitle.IndexOf(" wins by over", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? subtitle[..idx] : subtitle;
    }

    public static int FuzzyScore(string dbName, string kalshiName)
    {
        var raw = Fuzz.TokenSortRatio(dbName, kalshiName);
        // TokenSetRatio handles subset matching: "Flyers" ⊂ "Philadelphia Flyers" → 100
        var stripped = Fuzz.TokenSetRatio(dbName, StripAbbreviation(kalshiName));
        return Math.Max(raw, stripped);
    }

    public class MapDiagnostics
    {
        public int NonPairEvents { get; set; }
        public int NullExpirationEvents { get; set; }
        public int NoGameMatchEvents { get; set; }
        public int NoHomeAwayMatchEvents { get; set; }
        public int ZeroPriceEvents { get; set; }
        public List<string> SampleNoMatch { get; set; } = new();
    }

    public static (OddsApiResponseMapper.MappedOdds Odds, MapDiagnostics Diagnostics) MapWithDiagnostics(
        List<KalshiMarket> gameMarkets,
        List<KalshiMarket> spreadMarkets,
        List<KalshiMarket> totalMarkets,
        List<OddsApiResponseMapper.GameInfo> games)
    {
        var result = new OddsApiResponseMapper.MappedOdds();
        var diagnostics = new MapDiagnostics();
        var now = DateTime.UtcNow;

        MapMoneyline(gameMarkets, games, result, now, diagnostics);
        MapSpreads(spreadMarkets, games, result, now);
        MapTotals(totalMarkets, games, result, now);

        return (result, diagnostics);
    }

    public static OddsApiResponseMapper.MappedOdds Map(
        List<KalshiMarket> gameMarkets,
        List<KalshiMarket> spreadMarkets,
        List<KalshiMarket> totalMarkets,
        List<OddsApiResponseMapper.GameInfo> games)
    {
        return MapWithDiagnostics(gameMarkets, spreadMarkets, totalMarkets, games).Odds;
    }

    private static void MapMoneyline(
        List<KalshiMarket> markets,
        List<OddsApiResponseMapper.GameInfo> games,
        OddsApiResponseMapper.MappedOdds result,
        DateTime now,
        MapDiagnostics? diagnostics = null)
    {
        // Group by event ticker — each game has two markets (one per team)
        var byEvent = markets.GroupBy(m => m.EventTicker);

        foreach (var group in byEvent)
        {
            var marketList = group.ToList();
            if (marketList.Count != 2)
            {
                if (diagnostics != null) diagnostics.NonPairEvents++;
                continue;
            }

            var expiration = marketList[0].ExpectedExpirationTime;
            if (expiration == null)
            {
                if (diagnostics != null) diagnostics.NullExpirationEvents++;
                continue;
            }
            var centralDate = ToCentralDate(expiration.Value.ToUniversalTime());

            var gameId = MatchGameId(marketList, games, centralDate);
            if (gameId == null)
            {
                if (diagnostics != null)
                {
                    diagnostics.NoGameMatchEvents++;
                    if (diagnostics.SampleNoMatch.Count < 10)
                    {
                        var names = string.Join(" vs ", marketList.Select(m => m.YesSubTitle));
                        diagnostics.SampleNoMatch.Add($"{names} on {centralDate:yyyy-MM-dd} ({group.Key})");
                    }
                }
                continue;
            }

            // Determine which market is home and which is away
            var game = games.First(g => g.GameId == gameId.Value);
            var (homeMarket, awayMarket) = MatchHomeAway(marketList, game);
            if (homeMarket == null || awayMarket == null)
            {
                if (diagnostics != null) diagnostics.NoHomeAwayMatchEvents++;
                continue;
            }

            // Each h2h market is "Team wins?" — use yes_ask as implied probability (includes vig)
            var homeOdds = homeMarket.YesAsk;
            var awayOdds = awayMarket.YesAsk;

            if (homeOdds <= 0 || awayOdds <= 0)
            {
                if (diagnostics != null) diagnostics.ZeroPriceEvents++;
                continue;
            }

            result.H2H.Add(new DbBookmakerOdds
            {
                GameId = gameId.Value,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                HomeOdds = homeOdds,
                AwayOdds = awayOdds,
                FetchedDateUTC = now,
                BookmakerLastUpdate = homeMarket.UpdatedTime ?? now,
                MarketLastUpdate = homeMarket.UpdatedTime ?? now,
                OddsApiGameId = group.Key,
            });
        }
    }

    private static void MapSpreads(
        List<KalshiMarket> markets,
        List<OddsApiResponseMapper.GameInfo> games,
        OddsApiResponseMapper.MappedOdds result,
        DateTime now)
    {
        var byEvent = markets
            .Where(m => m.FloorStrike is STANDARD_PUCK_LINE)
            .GroupBy(m => m.EventTicker);

        foreach (var group in byEvent)
        {
            var marketList = group.ToList();
            if (marketList.Count != 2) continue;

            var expiration = marketList[0].ExpectedExpirationTime;
            if (expiration == null) continue;
            var centralDate = ToCentralDate(expiration.Value.ToUniversalTime());

            // Spread subtitles are "TeamName wins by over 1.5 goals" — extract just the team name
            var teamNames = marketList.Select(m => ExtractSpreadTeamName(m.YesSubTitle)).ToList();

            var gameId = MatchGameIdFromNames(teamNames, games, centralDate);
            if (gameId == null) continue;

            var game = games.First(g => g.GameId == gameId.Value);
            var (homeMarket, awayMarket) = MatchHomeAwayFromNames(marketList, game,
                m => ExtractSpreadTeamName(m.YesSubTitle));
            if (homeMarket == null || awayMarket == null) continue;

            // Single contract: "Home wins by over 1.5"
            // Buy YES (Home -1.5) at YesAsk price
            // Buy NO (Away +1.5) at 1 - YesBid price
            // The bid/ask spread is the vig
            result.Spreads.Add(new DbBookmakerSpreads
            {
                GameId = gameId.Value,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                HomePoint = -STANDARD_PUCK_LINE,
                HomePrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(homeMarket.YesAsk),
                AwayPoint = STANDARD_PUCK_LINE,
                AwayPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(1.0 - homeMarket.YesBid),
                FetchedDateUTC = now,
                BookmakerLastUpdate = homeMarket.UpdatedTime ?? now,
                MarketLastUpdate = homeMarket.UpdatedTime ?? now,
                OddsApiGameId = group.Key,
            });
        }
    }

    private static void MapTotals(
        List<KalshiMarket> markets,
        List<OddsApiResponseMapper.GameInfo> games,
        OddsApiResponseMapper.MappedOdds result,
        DateTime now)
    {
        // Pick the line closest to 5.5 per event
        var byEvent = markets.GroupBy(m => m.EventTicker);

        foreach (var group in byEvent)
        {
            // Each total line has one market (over X.5 goals)
            var bestLine = group
                .Where(m => m.FloorStrike.HasValue)
                .OrderBy(m => Math.Abs(m.FloorStrike!.Value - DEFAULT_TOTAL_LINE))
                .FirstOrDefault();

            if (bestLine == null) continue;

            var expiration = bestLine.ExpectedExpirationTime;
            if (expiration == null) continue;
            var centralDate = ToCentralDate(expiration.Value.ToUniversalTime());

            var gameId = MatchGameIdFromSingle(bestLine, games, centralDate);
            if (gameId == null) continue;

            // Single contract: "Over X.5 goals"
            // Buy YES (Over) at YesAsk, Buy NO (Under) at 1 - YesBid
            result.Totals.Add(new DbBookmakerTotals
            {
                GameId = gameId.Value,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                OverUnderPoint = bestLine.FloorStrike!.Value,
                OverPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(bestLine.YesAsk),
                UnderPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(1.0 - bestLine.YesBid),
                FetchedDateUTC = now,
                BookmakerLastUpdate = bestLine.UpdatedTime ?? now,
                MarketLastUpdate = bestLine.UpdatedTime ?? now,
                OddsApiGameId = group.Key,
            });
        }
    }

    /// <summary>
    /// Score a game against a pair of Kalshi team names by trying both home/away assignments
    /// and returning the best combined score. This prevents false matches where only one
    /// team name matches well.
    /// </summary>
    public static int PairScore(OddsApiResponseMapper.GameInfo game, List<string> teamNames)
    {
        if (teamNames.Count != 2) return 0;

        var t0 = teamNames[0].ToLower();
        var t1 = teamNames[1].ToLower();
        var home = game.HomeTeamName.ToLower();
        var away = game.AwayTeamName.ToLower();

        // Try both assignments: t0=home/t1=away vs t0=away/t1=home
        var score1 = FuzzyScore(home, t0) + FuzzyScore(away, t1);
        var score2 = FuzzyScore(home, t1) + FuzzyScore(away, t0);

        return Math.Max(score1, score2);
    }

    public static int? MatchGameId(
        List<KalshiMarket> marketPair,
        List<OddsApiResponseMapper.GameInfo> games,
        DateTime centralDate)
    {
        var teamNames = marketPair.Select(m => m.YesSubTitle).ToList();
        var prevDate = centralDate.AddDays(-1);
        var nextDate = centralDate.AddDays(1);

        var bestMatch = games
            .Where(g =>
            {
                var gameCentral = ToCentralDate(g.GameDateUTC);
                return gameCentral == centralDate || gameCentral == prevDate || gameCentral == nextDate;
            })
            .Select(g => new { Game = g, Score = PairScore(g, teamNames) })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        // Combined score of two teams — require average of 70 per team (140 total)
        return bestMatch is { Score: > 140 } ? bestMatch.Game.GameId : null;
    }

    /// <summary>
    /// Extract team names from a total market title like "Colorado vs Washington: Total Goals".
    /// Returns both team names, or falls back to the full title.
    /// </summary>
    public static List<string> ExtractTotalTeamNames(string title)
    {
        // Strip suffix like ": Total Goals" or ": Total Points"
        var colonIdx = title.IndexOf(':');
        var matchup = colonIdx >= 0 ? title[..colonIdx].Trim() : title;

        // Kalshi uses "vs" for newer titles and "at" for older ones
        var parts = matchup.Split(" vs ", StringSplitOptions.TrimEntries);
        if (parts.Length == 2) return parts.ToList();

        parts = matchup.Split(" at ", StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts.ToList() : new List<string> { title };
    }

    public static int? MatchGameIdFromSingle(
        KalshiMarket market,
        List<OddsApiResponseMapper.GameInfo> games,
        DateTime centralDate)
    {
        // Total markets have the game in the title: "Colorado vs Washington: Total Goals"
        var teamNames = ExtractTotalTeamNames(market.Title);
        var prevDate = centralDate.AddDays(-1);
        var nextDate = centralDate.AddDays(1);

        var bestMatch = games
            .Where(g =>
            {
                var gameCentral = ToCentralDate(g.GameDateUTC);
                return gameCentral == centralDate || gameCentral == prevDate || gameCentral == nextDate;
            })
            .Select(g => new { Game = g, Score = PairScore(g, teamNames) })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        // Totals use city names only (e.g. "Colorado" vs "Colorado Avalanche") so lower threshold
        return bestMatch is { Score: > 100 } ? bestMatch.Game.GameId : null;
    }

    public static (KalshiMarket? home, KalshiMarket? away) MatchHomeAway(
        List<KalshiMarket> marketPair,
        OddsApiResponseMapper.GameInfo game)
    {
        return MatchHomeAwayFromNames(marketPair, game, m => m.YesSubTitle);
    }

    public static int? MatchGameIdFromNames(
        List<string> teamNames,
        List<OddsApiResponseMapper.GameInfo> games,
        DateTime centralDate)
    {
        var prevDate = centralDate.AddDays(-1);
        var nextDate = centralDate.AddDays(1);

        var bestMatch = games
            .Where(g =>
            {
                var gameCentral = ToCentralDate(g.GameDateUTC);
                return gameCentral == centralDate || gameCentral == prevDate || gameCentral == nextDate;
            })
            .Select(g => new { Game = g, Score = PairScore(g, teamNames) })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return bestMatch is { Score: > 140 } ? bestMatch.Game.GameId : null;
    }

    public static (KalshiMarket? home, KalshiMarket? away) MatchHomeAwayFromNames(
        List<KalshiMarket> marketPair,
        OddsApiResponseMapper.GameInfo game,
        Func<KalshiMarket, string> nameExtractor)
    {
        if (marketPair.Count != 2) return (null, null);

        var m0 = marketPair[0];
        var m1 = marketPair[1];

        // Score both possible assignments and pick the better one
        var m0AsHome = FuzzyScore(game.HomeTeamName.ToLower(), nameExtractor(m0).ToLower())
                     + FuzzyScore(game.AwayTeamName.ToLower(), nameExtractor(m1).ToLower());
        var m1AsHome = FuzzyScore(game.HomeTeamName.ToLower(), nameExtractor(m1).ToLower())
                     + FuzzyScore(game.AwayTeamName.ToLower(), nameExtractor(m0).ToLower());

        return m0AsHome >= m1AsHome ? (m0, m1) : (m1, m0);
    }
}
