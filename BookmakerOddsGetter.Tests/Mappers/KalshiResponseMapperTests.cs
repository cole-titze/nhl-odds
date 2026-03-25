using Entities.ServiceModels.Kalshi;
using Entities.ServiceModels.Mappers;
using FluentAssertions;

namespace BookmakerOddsGetter.Tests.Mappers;

[TestClass]
public class KalshiResponseMapperTests
{
    private static readonly DateTime GameDate = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);

    private static List<OddsApiResponseMapper.GameInfo> CreateGames()
    {
        return new List<OddsApiResponseMapper.GameInfo>
        {
            new OddsApiResponseMapper.GameInfo
            {
                GameId = 2024020001,
                HomeTeamName = "Boston Bruins",
                AwayTeamName = "Montreal Canadiens",
                GameDateUTC = GameDate,
            },
        };
    }

    #region Moneyline

    [TestMethod]
    public void Map_Moneyline_WithValidPair_ShouldMapH2H()
    {
        var markets = new List<KalshiMarket>
        {
            CreateMarket("KXNHLGAME-EVT1", "KXNHLGAME-EVT1-T1", "Boston Bruins", "0.60", "0.62"),
            CreateMarket("KXNHLGAME-EVT1", "KXNHLGAME-EVT1-T2", "Montreal Canadiens", "0.38", "0.40"),
        };

        var result = KalshiResponseMapper.Map(markets, new List<KalshiMarket>(), new List<KalshiMarket>(), CreateGames());

        result.H2H.Should().HaveCount(1);
        result.H2H[0].GameId.Should().Be(2024020001);
        result.H2H[0].BookmakerName.Should().Be("Kalshi");
        result.H2H[0].HomeOdds.Should().BeApproximately(0.62, 0.01); // yes_ask
        result.H2H[0].AwayOdds.Should().BeApproximately(0.40, 0.01); // yes_ask
    }

    [TestMethod]
    public void Map_Moneyline_WithOddNumberOfMarkets_ShouldSkip()
    {
        var markets = new List<KalshiMarket>
        {
            CreateMarket("KXNHLGAME-EVT1", "KXNHLGAME-EVT1-T1", "Boston Bruins", "0.60", "0.62"),
            // Only one market for this event
        };

        var result = KalshiResponseMapper.Map(markets, new List<KalshiMarket>(), new List<KalshiMarket>(), CreateGames());

        result.H2H.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_Moneyline_WithNoMatchingGame_ShouldSkip()
    {
        var markets = new List<KalshiMarket>
        {
            CreateMarket("KXNHLGAME-EVT1", "T1", "Tampa Bay Lightning", "0.55", "0.57"),
            CreateMarket("KXNHLGAME-EVT1", "T2", "Florida Panthers", "0.43", "0.45"),
        };

        var result = KalshiResponseMapper.Map(markets, new List<KalshiMarket>(), new List<KalshiMarket>(), CreateGames());

        result.H2H.Should().BeEmpty();
    }

    #endregion

    #region Spreads

    [TestMethod]
    public void Map_Spreads_WithStandardPuckLine_ShouldMapSpreads()
    {
        var spreadMarkets = new List<KalshiMarket>
        {
            CreateMarket("KXNHLSPREAD-EVT1", "T1", "Boston Bruins", "0.35", "0.37", floorStrike: 1.5),
            CreateMarket("KXNHLSPREAD-EVT1", "T2", "Montreal Canadiens", "0.63", "0.65", floorStrike: 1.5),
        };

        var result = KalshiResponseMapper.Map(new List<KalshiMarket>(), spreadMarkets, new List<KalshiMarket>(), CreateGames());

        result.Spreads.Should().HaveCount(1);
        result.Spreads[0].HomePoint.Should().Be(-1.5);
        result.Spreads[0].AwayPoint.Should().Be(1.5);
        result.Spreads[0].BookmakerName.Should().Be("Kalshi");
    }

    [TestMethod]
    public void Map_Spreads_WithNonStandardPuckLine_ShouldSkip()
    {
        var spreadMarkets = new List<KalshiMarket>
        {
            CreateMarket("KXNHLSPREAD-EVT1", "T1", "Boston Bruins", "0.20", "0.22", floorStrike: 2.5),
            CreateMarket("KXNHLSPREAD-EVT1", "T2", "Montreal Canadiens", "0.78", "0.80", floorStrike: 2.5),
        };

        var result = KalshiResponseMapper.Map(new List<KalshiMarket>(), spreadMarkets, new List<KalshiMarket>(), CreateGames());

        result.Spreads.Should().BeEmpty();
    }

    #endregion

    #region Totals

    [TestMethod]
    public void Map_Totals_ShouldPickLineClosestTo5_5()
    {
        var totalMarkets = new List<KalshiMarket>
        {
            CreateTotalMarket("KXNHLTOTAL-EVT1", "T-4.5", 4.5, "0.70", "0.72"),
            CreateTotalMarket("KXNHLTOTAL-EVT1", "T-5.5", 5.5, "0.50", "0.52"),
            CreateTotalMarket("KXNHLTOTAL-EVT1", "T-6.5", 6.5, "0.30", "0.32"),
        };

        var result = KalshiResponseMapper.Map(new List<KalshiMarket>(), new List<KalshiMarket>(), totalMarkets, CreateGames());

        result.Totals.Should().HaveCount(1);
        result.Totals[0].OverUnderPoint.Should().Be(5.5);
        result.Totals[0].BookmakerName.Should().Be("Kalshi");
    }

    [TestMethod]
    public void Map_Totals_WithNoMarkets_ShouldReturnEmpty()
    {
        var result = KalshiResponseMapper.Map(
            new List<KalshiMarket>(), new List<KalshiMarket>(), new List<KalshiMarket>(), CreateGames());

        result.Totals.Should().BeEmpty();
    }

    #endregion

    #region Full Map

    [TestMethod]
    public void Map_WithAllMarketTypes_ShouldMapAll()
    {
        var moneyline = new List<KalshiMarket>
        {
            CreateMarket("KXNHLGAME-EVT1", "T1", "Boston Bruins", "0.60", "0.62"),
            CreateMarket("KXNHLGAME-EVT1", "T2", "Montreal Canadiens", "0.38", "0.40"),
        };
        var spreads = new List<KalshiMarket>
        {
            CreateMarket("KXNHLSPREAD-EVT1", "T1", "Boston Bruins", "0.35", "0.37", floorStrike: 1.5),
            CreateMarket("KXNHLSPREAD-EVT1", "T2", "Montreal Canadiens", "0.63", "0.65", floorStrike: 1.5),
        };
        var totals = new List<KalshiMarket>
        {
            CreateTotalMarket("KXNHLTOTAL-EVT1", "T-5.5", 5.5, "0.50", "0.52"),
        };

        var result = KalshiResponseMapper.Map(moneyline, spreads, totals, CreateGames());

        result.H2H.Should().HaveCount(1);
        result.Spreads.Should().HaveCount(1);
        result.Totals.Should().HaveCount(1);
    }

    #endregion

    #region Helpers

    private static KalshiMarket CreateMarket(string eventTicker, string ticker, string yesSubTitle,
        string yesBid, string yesAsk, double? floorStrike = null)
    {
        return new KalshiMarket
        {
            EventTicker = eventTicker,
            Ticker = ticker,
            Title = $"{yesSubTitle} Game",
            YesSubTitle = yesSubTitle,
            YesBidDollars = yesBid,
            YesAskDollars = yesAsk,
            FloorStrike = floorStrike,
            ExpectedExpirationTime = GameDate.AddHours(12),
            UpdatedTime = DateTime.UtcNow,
        };
    }

    private static KalshiMarket CreateTotalMarket(string eventTicker, string ticker,
        double floorStrike, string yesBid, string yesAsk)
    {
        return new KalshiMarket
        {
            EventTicker = eventTicker,
            Ticker = ticker,
            Title = "Boston Bruins vs Montreal Canadiens: Total Goals",
            YesSubTitle = $"Over {floorStrike}",
            YesBidDollars = yesBid,
            YesAskDollars = yesAsk,
            FloorStrike = floorStrike,
            ExpectedExpirationTime = GameDate.AddHours(12),
            UpdatedTime = DateTime.UtcNow,
        };
    }

    #endregion
}
