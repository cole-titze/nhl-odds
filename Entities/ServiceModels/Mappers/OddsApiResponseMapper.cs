using Entities.DbModels;
using Entities.ServiceModels.OddsApi;
using FuzzySharp;

namespace Entities.ServiceModels.Mappers;

public static class OddsApiResponseMapper
{
    public class GameInfo
    {
        public int GameId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public DateTime GameDateUTC { get; set; }
    }

    public class MappedOdds
    {
        public List<DbBookmakerOdds> H2H { get; set; } = new();
        public List<DbBookmakerSpreads> Spreads { get; set; } = new();
        public List<DbBookmakerTotals> Totals { get; set; } = new();
    }

    public static MappedOdds Map(List<OddsApiResponse> responses, List<GameInfo> games,
        DateTime? asOfUtc = null)
    {
        var result = new MappedOdds();
        var cutoff = asOfUtc ?? DateTime.UtcNow;

        foreach (var response in responses)
        {
            // Skip games that have already commenced — their odds are live/in-play, not pre-game
            if (response.CommenceTime.ToUniversalTime() <= cutoff)
                continue;

            var gameId = MatchGameId(response, games);
            if (gameId == null)
                continue;

            var now = DateTime.UtcNow;

            foreach (var bookmaker in response.Bookmakers)
            {
                foreach (var market in bookmaker.Markets)
                {
                    switch (market.Key)
                    {
                        case "h2h":
                            var h2h = MapH2H(response, gameId.Value, bookmaker, market, now);
                            if (h2h != null)
                                result.H2H.Add(h2h);
                            break;
                        case "spreads":
                            var spread = MapSpreads(response, gameId.Value, bookmaker, market, now);
                            if (spread != null)
                                result.Spreads.Add(spread);
                            break;
                        case "totals":
                            var total = MapTotals(gameId.Value, bookmaker, market, now, response.Id);
                            if (total != null)
                                result.Totals.Add(total);
                            break;
                    }
                }
            }
        }

        return result;
    }

    private static DbBookmakerOdds? MapH2H(OddsApiResponse response, int gameId,
        OddsApiBookmaker bookmaker, OddsApiMarket market, DateTime now)
    {
        var homeOutcome = market.Outcomes.FirstOrDefault(o =>
            Fuzz.TokenSortRatio(o.Name.ToLower(), response.HomeTeam.ToLower()) > 70);
        var awayOutcome = market.Outcomes.FirstOrDefault(o =>
            Fuzz.TokenSortRatio(o.Name.ToLower(), response.AwayTeam.ToLower()) > 70);

        if (homeOutcome == null || awayOutcome == null)
            return null;

        return new DbBookmakerOdds
        {
            GameId = gameId,
            BookmakerName = bookmaker.Title,
            BookmakerKey = bookmaker.Key,
            HomeOdds = AmericanToImpliedProbability(homeOutcome.Price),
            AwayOdds = AmericanToImpliedProbability(awayOutcome.Price),
            FetchedDateUTC = now,
            BookmakerLastUpdate = bookmaker.LastUpdate,
            MarketLastUpdate = market.LastUpdate,
            OddsApiGameId = response.Id,
        };
    }

    private static DbBookmakerSpreads? MapSpreads(OddsApiResponse response, int gameId,
        OddsApiBookmaker bookmaker, OddsApiMarket market, DateTime now)
    {
        var homeOutcome = market.Outcomes.FirstOrDefault(o =>
            Fuzz.TokenSortRatio(o.Name.ToLower(), response.HomeTeam.ToLower()) > 70);
        var awayOutcome = market.Outcomes.FirstOrDefault(o =>
            Fuzz.TokenSortRatio(o.Name.ToLower(), response.AwayTeam.ToLower()) > 70);

        if (homeOutcome == null || awayOutcome == null || !homeOutcome.Point.HasValue || !awayOutcome.Point.HasValue)
            return null;

        return new DbBookmakerSpreads
        {
            GameId = gameId,
            BookmakerName = bookmaker.Title,
            BookmakerKey = bookmaker.Key,
            HomePoint = homeOutcome.Point.Value,
            HomePrice = homeOutcome.Price,
            AwayPoint = awayOutcome.Point.Value,
            AwayPrice = awayOutcome.Price,
            FetchedDateUTC = now,
            BookmakerLastUpdate = bookmaker.LastUpdate,
            MarketLastUpdate = market.LastUpdate,
            OddsApiGameId = response.Id,
        };
    }

    private static DbBookmakerTotals? MapTotals(int gameId,
        OddsApiBookmaker bookmaker, OddsApiMarket market, DateTime now, string oddsApiGameId)
    {
        var overOutcome = market.Outcomes.FirstOrDefault(o => o.Name == "Over");
        var underOutcome = market.Outcomes.FirstOrDefault(o => o.Name == "Under");

        if (overOutcome == null || underOutcome == null || !overOutcome.Point.HasValue)
            return null;

        return new DbBookmakerTotals
        {
            GameId = gameId,
            BookmakerName = bookmaker.Title,
            BookmakerKey = bookmaker.Key,
            OverUnderPoint = overOutcome.Point.Value,
            OverPrice = overOutcome.Price,
            UnderPrice = underOutcome.Price,
            FetchedDateUTC = now,
            BookmakerLastUpdate = bookmaker.LastUpdate,
            MarketLastUpdate = market.LastUpdate,
            OddsApiGameId = oddsApiGameId,
        };
    }

    private static int FuzzyScore(string dbName, string apiName)
    {
        var sort = Fuzz.TokenSortRatio(dbName, apiName);
        var set = Fuzz.TokenSetRatio(dbName, apiName);
        return Math.Max(sort, set);
    }

    private static int? MatchGameId(OddsApiResponse response, List<GameInfo> games)
    {
        // Compare dates in UTC to avoid timezone issues
        // Check previous and next day — game dates can be off by a day due to timezones
        var apiDateUtc = response.CommenceTime.ToUniversalTime().Date;
        var prevDate = apiDateUtc.AddDays(-1);
        var nextDate = apiDateUtc.AddDays(1);

        var bestMatch = games
            .Where(g => g.GameDateUTC.Date == apiDateUtc || g.GameDateUTC.Date == prevDate || g.GameDateUTC.Date == nextDate)
            .Select(g =>
            {
                var homeHome = FuzzyScore(g.HomeTeamName.ToLower(), response.HomeTeam.ToLower());
                var awayAway = FuzzyScore(g.AwayTeamName.ToLower(), response.AwayTeam.ToLower());
                var normalScore = Math.Min(homeHome, awayAway);

                // Also check swapped home/away — API and DB can disagree
                var homeCross = FuzzyScore(g.HomeTeamName.ToLower(), response.AwayTeam.ToLower());
                var awayCross = FuzzyScore(g.AwayTeamName.ToLower(), response.HomeTeam.ToLower());
                var crossScore = Math.Min(homeCross, awayCross);

                return new { Game = g, Score = Math.Max(normalScore, crossScore) };
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (bestMatch != null && bestMatch.Score > 70)
            return bestMatch.Game.GameId;

        return null;
    }

    public static double AmericanToImpliedProbability(int americanOdds)
    {
        if (americanOdds < 0)
            return Math.Abs(americanOdds) / (Math.Abs(americanOdds) + 100.0);
        else
            return 100.0 / (americanOdds + 100.0);
    }

    public static int ImpliedProbabilityToAmerican(double prob)
    {
        if (prob <= 0 || prob >= 1)
            return prob >= 1 ? -10000 : 10000;
        if (prob >= 0.5)
            return (int)Math.Round(-(prob / (1.0 - prob)) * 100.0);
        else
            return (int)Math.Round((1.0 - prob) / prob * 100.0);
    }
}