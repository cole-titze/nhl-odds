using System.Text.Json.Serialization;

namespace Entities.ServiceModels.Kalshi;

public class KalshiMarket
{
    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = string.Empty;
    [JsonPropertyName("event_ticker")]
    public string EventTicker { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("yes_sub_title")]
    public string YesSubTitle { get; set; } = string.Empty;
    [JsonPropertyName("no_sub_title")]
    public string NoSubTitle { get; set; } = string.Empty;
    [JsonPropertyName("yes_bid_dollars")]
    public string YesBidDollars { get; set; } = "0";
    [JsonPropertyName("yes_ask_dollars")]
    public string YesAskDollars { get; set; } = "0";
    [JsonPropertyName("no_bid_dollars")]
    public string NoBidDollars { get; set; } = "0";
    [JsonPropertyName("no_ask_dollars")]
    public string NoAskDollars { get; set; } = "0";
    [JsonPropertyName("last_price_dollars")]
    public string LastPriceDollars { get; set; } = "0";
    [JsonPropertyName("floor_strike")]
    public double? FloorStrike { get; set; }
    [JsonPropertyName("expected_expiration_time")]
    public DateTime? ExpectedExpirationTime { get; set; }
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("updated_time")]
    public DateTime? UpdatedTime { get; set; }

    public double YesBid => double.TryParse(YesBidDollars, out var v) ? v : 0;
    public double YesAsk => double.TryParse(YesAskDollars, out var v) ? v : 0;
    public double LastPrice => double.TryParse(LastPriceDollars, out var v) ? v : 0;
    public double MidPrice
    {
        get
        {
            var mid = (YesBid + YesAsk) / 2.0;
            // Settled markets have zeroed bid/ask — fall back to last traded price
            return mid > 0 ? mid : LastPrice;
        }
    }
}

public class KalshiMarketsResponse
{
    [JsonPropertyName("markets")]
    public List<KalshiMarket> Markets { get; set; } = new();
    [JsonPropertyName("cursor")]
    public string Cursor { get; set; } = string.Empty;
}

public class KalshiOhlc
{
    [JsonPropertyName("open_dollars")]
    public string OpenDollars { get; set; } = "0";
    [JsonPropertyName("high_dollars")]
    public string HighDollars { get; set; } = "0";
    [JsonPropertyName("low_dollars")]
    public string LowDollars { get; set; } = "0";
    [JsonPropertyName("close_dollars")]
    public string CloseDollars { get; set; } = "0";

    public double Close => double.TryParse(CloseDollars, out var v) ? v : 0;
}

public class KalshiCandlestick
{
    [JsonPropertyName("end_period_ts")]
    public long EndPeriodTs { get; set; }
    [JsonPropertyName("yes_bid")]
    public KalshiOhlc YesBid { get; set; } = new();
    [JsonPropertyName("yes_ask")]
    public KalshiOhlc YesAsk { get; set; } = new();
    [JsonPropertyName("price")]
    public KalshiOhlc Price { get; set; } = new();
    [JsonPropertyName("volume_fp")]
    public string VolumeFp { get; set; } = "0";

    public double MidPrice
    {
        get
        {
            var mid = (YesBid.Close + YesAsk.Close) / 2.0;
            return mid > 0 ? mid : Price.Close;
        }
    }
}

public class KalshiCandlestickResponse
{
    [JsonPropertyName("candlesticks")]
    public List<KalshiCandlestick> Candlesticks { get; set; } = new();
    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = string.Empty;
}

public class KalshiCutoffResponse
{
    [JsonPropertyName("market_settled_ts")]
    public DateTime? MarketSettledTs { get; set; }
    [JsonPropertyName("trades_created_ts")]
    public DateTime? TradesCreatedTs { get; set; }
    [JsonPropertyName("orders_updated_ts")]
    public DateTime? OrdersUpdatedTs { get; set; }
}