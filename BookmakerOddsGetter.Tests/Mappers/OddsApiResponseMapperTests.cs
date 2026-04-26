using Entities.ServiceModels.Mappers;
using Entities.ServiceModels.OddsApi;
using FluentAssertions;

namespace BookmakerOddsGetter.Tests.Mappers;

[TestClass]
public class OddsApiResponseMapperTests
{
    private static readonly DateTime FutureDate = DateTime.UtcNow.AddDays(1);

    #region AmericanToImpliedProbability

    [TestMethod]
    public void AmericanToImpliedProbability_NegativeOdds_ShouldReturnCorrectProbability()
    {
        // -150: abs(150)/(150+100) = 150/250 = 0.6
        var result = OddsApiResponseMapper.AmericanToImpliedProbability(-150);
        result.Should().BeApproximately(0.6, 0.001);
    }

    [TestMethod]
    public void AmericanToImpliedProbability_PositiveOdds_ShouldReturnCorrectProbability()
    {
        // +200: 100/(200+100) = 100/300 = 0.333
        var result = OddsApiResponseMapper.AmericanToImpliedProbability(200);
        result.Should().BeApproximately(0.333, 0.001);
    }

    [TestMethod]
    public void AmericanToImpliedProbability_Even_ShouldReturnHalf()
    {
        // +100: 100/(100+100) = 0.5
        var result = OddsApiResponseMapper.AmericanToImpliedProbability(100);
        result.Should().BeApproximately(0.5, 0.001);
    }

    [TestMethod]
    public void AmericanToImpliedProbability_HeavyFavorite_ShouldReturnHighProbability()
    {
        // -500: 500/600 = 0.833
        var result = OddsApiResponseMapper.AmericanToImpliedProbability(-500);
        result.Should().BeApproximately(0.833, 0.001);
    }

    [TestMethod]
    public void AmericanToImpliedProbability_HeavyUnderdog_ShouldReturnLowProbability()
    {
        // +500: 100/600 = 0.167
        var result = OddsApiResponseMapper.AmericanToImpliedProbability(500);
        result.Should().BeApproximately(0.167, 0.001);
    }

    #endregion

    #region ImpliedProbabilityToAmerican

    [TestMethod]
    public void ImpliedProbabilityToAmerican_Favorite_ShouldReturnNegativeOdds()
    {
        // 0.6 -> -(0.6/0.4)*100 = -150
        var result = OddsApiResponseMapper.ImpliedProbabilityToAmerican(0.6);
        result.Should().Be(-150);
    }

    [TestMethod]
    public void ImpliedProbabilityToAmerican_Underdog_ShouldReturnPositiveOdds()
    {
        // 0.333 -> (0.667/0.333)*100 = 200
        var result = OddsApiResponseMapper.ImpliedProbabilityToAmerican(1.0 / 3.0);
        result.Should().Be(200);
    }

    [TestMethod]
    public void ImpliedProbabilityToAmerican_Even_ShouldReturnNeg100()
    {
        // 0.5 -> -(0.5/0.5)*100 = -100
        var result = OddsApiResponseMapper.ImpliedProbabilityToAmerican(0.5);
        result.Should().Be(-100);
    }

    [TestMethod]
    public void ImpliedProbabilityToAmerican_EdgeCase_ZeroProbability()
    {
        var result = OddsApiResponseMapper.ImpliedProbabilityToAmerican(0);
        result.Should().Be(10000);
    }

    [TestMethod]
    public void ImpliedProbabilityToAmerican_EdgeCase_OneProbability()
    {
        var result = OddsApiResponseMapper.ImpliedProbabilityToAmerican(1.0);
        result.Should().Be(-10000);
    }

    #endregion

    #region Roundtrip

    [TestMethod]
    [DataRow(-150)]
    [DataRow(-110)]
    [DataRow(-100)]
    [DataRow(150)]
    [DataRow(300)]
    public void AmericanOdds_Roundtrip_ShouldBeConsistent(int americanOdds)
    {
        var prob = OddsApiResponseMapper.AmericanToImpliedProbability(americanOdds);
        var roundtripped = OddsApiResponseMapper.ImpliedProbabilityToAmerican(prob);
        roundtripped.Should().BeCloseTo(americanOdds, 1);
    }

    #endregion

    #region Map

    [TestMethod]
    public void Map_WithAlreadyCommencedGame_ShouldSkip()
    {
        var pastDate = DateTime.UtcNow.AddHours(-1);
        var responses = new List<OddsApiResponse>
        {
            CreateResponse("Boston Bruins", "Montreal Canadiens", pastDate),
        };
        var games = new List<OddsApiResponseMapper.GameInfo>
        {
            CreateGameInfo(1, "Boston Bruins", "Montreal Canadiens", pastDate),
        };

        var result = OddsApiResponseMapper.Map(responses, games);

        result.H2H.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_WithFutureGame_ShouldMapH2H()
    {
        var responses = new List<OddsApiResponse>
        {
            CreateResponse("Boston Bruins", "Montreal Canadiens", FutureDate,
                h2hHome: -150, h2hAway: 130),
        };
        var games = new List<OddsApiResponseMapper.GameInfo>
        {
            CreateGameInfo(2024020001, "Boston Bruins", "Montreal Canadiens", FutureDate),
        };

        var result = OddsApiResponseMapper.Map(responses, games);

        result.H2H.Should().HaveCount(1);
        result.H2H[0].GameId.Should().Be(2024020001);
        result.H2H[0].HomeOdds.Should().BeApproximately(0.6, 0.01);
        result.H2H[0].AwayOdds.Should().BeApproximately(0.435, 0.01);
    }

    [TestMethod]
    public void Map_WithSpreads_ShouldMapSpreadData()
    {
        var responses = new List<OddsApiResponse>
        {
            CreateResponse("Boston Bruins", "Montreal Canadiens", FutureDate,
                spreadHome: -1.5, spreadHomePrice: -110, spreadAway: 1.5, spreadAwayPrice: -110),
        };
        var games = new List<OddsApiResponseMapper.GameInfo>
        {
            CreateGameInfo(2024020001, "Boston Bruins", "Montreal Canadiens", FutureDate),
        };

        var result = OddsApiResponseMapper.Map(responses, games);

        result.Spreads.Should().HaveCount(1);
        result.Spreads[0].HomePoint.Should().Be(-1.5);
        result.Spreads[0].AwayPoint.Should().Be(1.5);
    }

    [TestMethod]
    public void Map_WithTotals_ShouldMapTotalData()
    {
        var responses = new List<OddsApiResponse>
        {
            CreateResponse("Boston Bruins", "Montreal Canadiens", FutureDate,
                totalPoint: 6.0, overPrice: -115, underPrice: -105),
        };
        var games = new List<OddsApiResponseMapper.GameInfo>
        {
            CreateGameInfo(2024020001, "Boston Bruins", "Montreal Canadiens", FutureDate),
        };

        var result = OddsApiResponseMapper.Map(responses, games);

        result.Totals.Should().HaveCount(1);
        result.Totals[0].OverUnderPoint.Should().Be(6.0);
        result.Totals[0].OverPrice.Should().Be(-115);
        result.Totals[0].UnderPrice.Should().Be(-105);
    }

    [TestMethod]
    public void Map_WithNoMatchingGame_ShouldReturnEmpty()
    {
        var responses = new List<OddsApiResponse>
        {
            CreateResponse("Team A", "Team B", FutureDate, h2hHome: -150, h2hAway: 130),
        };
        var games = new List<OddsApiResponseMapper.GameInfo>
        {
            CreateGameInfo(1, "Completely Different", "Also Different", FutureDate),
        };

        var result = OddsApiResponseMapper.Map(responses, games);

        result.H2H.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_WithSwappedHomeAway_ShouldStillMatch()
    {
        // API has teams swapped compared to DB
        var responses = new List<OddsApiResponse>
        {
            CreateResponse("Montreal Canadiens", "Boston Bruins", FutureDate,
                h2hHome: -120, h2hAway: 100),
        };
        var games = new List<OddsApiResponseMapper.GameInfo>
        {
            CreateGameInfo(2024020001, "Boston Bruins", "Montreal Canadiens", FutureDate),
        };

        var result = OddsApiResponseMapper.Map(responses, games);

        result.H2H.Should().HaveCount(1);
        result.H2H[0].GameId.Should().Be(2024020001);
    }

    [TestMethod]
    public void Map_WithDateOffByOneDay_ShouldStillMatch()
    {
        var apiDate = FutureDate.AddDays(1);
        var responses = new List<OddsApiResponse>
        {
            CreateResponse("Boston Bruins", "Montreal Canadiens", apiDate,
                h2hHome: -150, h2hAway: 130),
        };
        var games = new List<OddsApiResponseMapper.GameInfo>
        {
            CreateGameInfo(2024020001, "Boston Bruins", "Montreal Canadiens", FutureDate),
        };

        // Use asOfUtc before the apiDate so the game isn't filtered as "commenced"
        var result = OddsApiResponseMapper.Map(responses, games, asOfUtc: DateTime.UtcNow);

        result.H2H.Should().HaveCount(1);
    }

    #endregion

    #region Helpers

    private static OddsApiResponseMapper.GameInfo CreateGameInfo(int gameId, string home, string away, DateTime date)
    {
        return new OddsApiResponseMapper.GameInfo
        {
            GameId = gameId,
            HomeTeamName = home,
            AwayTeamName = away,
            GameDateUTC = date,
        };
    }

    private static OddsApiResponse CreateResponse(string homeTeam, string awayTeam, DateTime commenceTime,
        int h2hHome = 0, int h2hAway = 0,
        double spreadHome = 0, int spreadHomePrice = 0, double spreadAway = 0, int spreadAwayPrice = 0,
        double totalPoint = 0, int overPrice = 0, int underPrice = 0)
    {
        var bookmaker = new OddsApiBookmaker
        {
            Key = "draftkings",
            Title = "DraftKings",
            LastUpdate = DateTime.UtcNow,
            Markets = new List<OddsApiMarket>(),
        };

        if (h2hHome != 0 || h2hAway != 0)
        {
            bookmaker.Markets.Add(new OddsApiMarket
            {
                Key = "h2h",
                LastUpdate = DateTime.UtcNow,
                Outcomes = new List<OddsApiOutcome>
                {
                    new OddsApiOutcome { Name = homeTeam, Price = h2hHome },
                    new OddsApiOutcome { Name = awayTeam, Price = h2hAway },
                },
            });
        }

        if (spreadHomePrice != 0)
        {
            bookmaker.Markets.Add(new OddsApiMarket
            {
                Key = "spreads",
                LastUpdate = DateTime.UtcNow,
                Outcomes = new List<OddsApiOutcome>
                {
                    new OddsApiOutcome { Name = homeTeam, Price = spreadHomePrice, Point = spreadHome },
                    new OddsApiOutcome { Name = awayTeam, Price = spreadAwayPrice, Point = spreadAway },
                },
            });
        }

        if (overPrice != 0)
        {
            bookmaker.Markets.Add(new OddsApiMarket
            {
                Key = "totals",
                LastUpdate = DateTime.UtcNow,
                Outcomes = new List<OddsApiOutcome>
                {
                    new OddsApiOutcome { Name = "Over", Price = overPrice, Point = totalPoint },
                    new OddsApiOutcome { Name = "Under", Price = underPrice, Point = totalPoint },
                },
            });
        }

        return new OddsApiResponse
        {
            Id = Guid.NewGuid().ToString(),
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            CommenceTime = commenceTime,
            Bookmakers = new List<OddsApiBookmaker> { bookmaker },
        };
    }

    #endregion
}