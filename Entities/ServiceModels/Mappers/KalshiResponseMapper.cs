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

    public static OddsApiResponseMapper.MappedOdds Map(
        List<KalshiMarket> gameMarkets,
        List<KalshiMarket> spreadMarkets,
        List<KalshiMarket> totalMarkets,
        List<OddsApiResponseMapper.GameInfo> games)
    {
        var result = new OddsApiResponseMapper.MappedOdds();
        var now = DateTime.UtcNow;

        MapMoneyline(gameMarkets, games, result, now);
        MapSpreads(spreadMarkets, games, result, now);
        MapTotals(totalMarkets, games, result, now);

        return result;
    }

    private static void MapMoneyline(
        List<KalshiMarket> markets,
        List<OddsApiResponseMapper.GameInfo> games,
        OddsApiResponseMapper.MappedOdds result,
        DateTime now)
    {
        // Group by event ticker — each game has two markets (one per team)
        var byEvent = markets.GroupBy(m => m.EventTicker);

        foreach (var group in byEvent)
        {
            var marketList = group.ToList();
            if (marketList.Count != 2) continue;

            var gameDate = marketList[0].ExpectedExpirationTime?.ToUniversalTime().Date;
            if (gameDate == null) continue;

            var gameId = MatchGameId(marketList, games, gameDate.Value);
            if (gameId == null) continue;

            // Determine which market is home and which is away
            var game = games.First(g => g.GameId == gameId.Value);
            var (homeMarket, awayMarket) = MatchHomeAway(marketList, game);
            if (homeMarket == null || awayMarket == null) continue;

            var homeOdds = homeMarket.MidPrice;
            var awayOdds = awayMarket.MidPrice;

            if (homeOdds <= 0 || awayOdds <= 0) continue;

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

            var gameDate = marketList[0].ExpectedExpirationTime?.ToUniversalTime().Date;
            if (gameDate == null) continue;

            var gameId = MatchGameId(marketList, games, gameDate.Value);
            if (gameId == null) continue;

            var game = games.First(g => g.GameId == gameId.Value);
            var (homeMarket, awayMarket) = MatchHomeAway(marketList, game);
            if (homeMarket == null || awayMarket == null) continue;

            // Home market: "team wins by over 1.5" = home covers -1.5
            // So homePoint = -1.5 (favorite), awayPoint = +1.5 (underdog)
            var homeProb = homeMarket.MidPrice;
            var awayProb = awayMarket.MidPrice;

            result.Spreads.Add(new DbBookmakerSpreads
            {
                GameId = gameId.Value,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                HomePoint = -STANDARD_PUCK_LINE,
                HomePrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(homeProb),
                AwayPoint = STANDARD_PUCK_LINE,
                AwayPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(awayProb),
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

            var gameDate = bestLine.ExpectedExpirationTime?.ToUniversalTime().Date;
            if (gameDate == null) continue;

            var gameId = MatchGameIdFromSingle(bestLine, games, gameDate.Value);
            if (gameId == null) continue;

            var overProb = bestLine.MidPrice;
            var underProb = 1.0 - overProb;

            result.Totals.Add(new DbBookmakerTotals
            {
                GameId = gameId.Value,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                OverUnderPoint = bestLine.FloorStrike!.Value,
                OverPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(overProb),
                UnderPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(underProb),
                FetchedDateUTC = now,
                BookmakerLastUpdate = bestLine.UpdatedTime ?? now,
                MarketLastUpdate = bestLine.UpdatedTime ?? now,
                OddsApiGameId = group.Key,
            });
        }
    }

    private static int? MatchGameId(
        List<KalshiMarket> marketPair,
        List<OddsApiResponseMapper.GameInfo> games,
        DateTime apiDateUtc)
    {
        var teamNames = marketPair.Select(m => m.YesSubTitle).ToList();
        var prevDate = apiDateUtc.AddDays(-1);
        var nextDate = apiDateUtc.AddDays(1);

        var bestMatch = games
            .Where(g => g.GameDateUTC.Date == apiDateUtc || g.GameDateUTC.Date == prevDate || g.GameDateUTC.Date == nextDate)
            .Select(g =>
            {
                var bestScore = 0;
                foreach (var teamName in teamNames)
                {
                    var homeScore = Fuzz.TokenSortRatio(g.HomeTeamName.ToLower(), teamName.ToLower());
                    var awayScore = Fuzz.TokenSortRatio(g.AwayTeamName.ToLower(), teamName.ToLower());
                    bestScore = Math.Max(bestScore, Math.Max(homeScore, awayScore));
                }
                return new { Game = g, Score = bestScore };
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return bestMatch is { Score: > 70 } ? bestMatch.Game.GameId : null;
    }

    private static int? MatchGameIdFromSingle(
        KalshiMarket market,
        List<OddsApiResponseMapper.GameInfo> games,
        DateTime apiDateUtc)
    {
        // Total markets have the game in the title: "Edmonton vs Utah: Total Goals"
        var title = market.Title.ToLower();
        var prevDate = apiDateUtc.AddDays(-1);
        var nextDate = apiDateUtc.AddDays(1);

        var bestMatch = games
            .Where(g => g.GameDateUTC.Date == apiDateUtc || g.GameDateUTC.Date == prevDate || g.GameDateUTC.Date == nextDate)
            .Select(g =>
            {
                var homeScore = Fuzz.TokenSortRatio(g.HomeTeamName.ToLower(), title);
                var awayScore = Fuzz.TokenSortRatio(g.AwayTeamName.ToLower(), title);
                return new { Game = g, Score = Math.Max(homeScore, awayScore) };
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return bestMatch is { Score: > 50 } ? bestMatch.Game.GameId : null;
    }

    private static (KalshiMarket? home, KalshiMarket? away) MatchHomeAway(
        List<KalshiMarket> marketPair,
        OddsApiResponseMapper.GameInfo game)
    {
        KalshiMarket? home = null, away = null;

        foreach (var m in marketPair)
        {
            var homeScore = Fuzz.TokenSortRatio(game.HomeTeamName.ToLower(), m.YesSubTitle.ToLower());
            var awayScore = Fuzz.TokenSortRatio(game.AwayTeamName.ToLower(), m.YesSubTitle.ToLower());

            if (homeScore > awayScore)
                home = m;
            else
                away = m;
        }

        return (home, away);
    }
}
