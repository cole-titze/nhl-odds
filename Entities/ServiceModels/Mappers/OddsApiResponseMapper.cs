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
        public DateTime GameDateUTC { get; set; }
    }

    public static List<DbBookmakerOdds> Map(List<OddsApiResponse> responses, List<GameInfo> games)
    {
        var results = new List<DbBookmakerOdds>();

        foreach (var response in responses)
        {
            var gameId = MatchGameId(response, games);
            if (gameId == null)
                continue;

            foreach (var bookmaker in response.Bookmakers)
            {
                var h2hMarket = bookmaker.Markets.FirstOrDefault(m => m.Key == "h2h");
                if (h2hMarket == null)
                    continue;

                var homeOutcome = h2hMarket.Outcomes.FirstOrDefault(o =>
                    Fuzz.TokenSortRatio(o.Name.ToLower(), response.HomeTeam.ToLower()) > 70);
                var awayOutcome = h2hMarket.Outcomes.FirstOrDefault(o =>
                    Fuzz.TokenSortRatio(o.Name.ToLower(), response.AwayTeam.ToLower()) > 70);

                if (homeOutcome == null || awayOutcome == null)
                    continue;

                results.Add(new DbBookmakerOdds
                {
                    GameId = gameId.Value,
                    BookmakerName = bookmaker.Title,
                    HomeOdds = AmericanToImpliedProbability(homeOutcome.Price),
                    AwayOdds = AmericanToImpliedProbability(awayOutcome.Price),
                    FetchedDateUTC = DateTime.UtcNow,
                });
            }
        }

        return results;
    }

    private static int? MatchGameId(OddsApiResponse response, List<GameInfo> games)
    {
        // Compare dates in UTC to avoid timezone issues
        var apiDateUtc = response.CommenceTime.ToUniversalTime().Date;

        var bestMatch = games
            .Where(g => g.GameDateUTC.Date == apiDateUtc)
            .Select(g => new { Game = g, Score = Fuzz.TokenSortRatio(g.HomeTeamName.ToLower(), response.HomeTeam.ToLower()) })
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
}
