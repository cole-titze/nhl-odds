using Newtonsoft.Json;

namespace Entities.ServiceModels.Kalshi;

public class KalshiMarket
{
    [JsonProperty("ticker")]
    public string Ticker { get; set; } = string.Empty;
    [JsonProperty("event_ticker")]
    public string EventTicker { get; set; } = string.Empty;
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty("yes_sub_title")]
    public string YesSubTitle { get; set; } = string.Empty;
    [JsonProperty("no_sub_title")]
    public string NoSubTitle { get; set; } = string.Empty;
    [JsonProperty("yes_bid_dollars")]
    public string YesBidDollars { get; set; } = "0";
    [JsonProperty("yes_ask_dollars")]
    public string YesAskDollars { get; set; } = "0";
    [JsonProperty("no_bid_dollars")]
    public string NoBidDollars { get; set; } = "0";
    [JsonProperty("no_ask_dollars")]
    public string NoAskDollars { get; set; } = "0";
    [JsonProperty("last_price_dollars")]
    public string LastPriceDollars { get; set; } = "0";
    [JsonProperty("floor_strike")]
    public double? FloorStrike { get; set; }
    [JsonProperty("expected_expiration_time")]
    public DateTime? ExpectedExpirationTime { get; set; }
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;
    [JsonProperty("updated_time")]
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
    [JsonProperty("markets")]
    public List<KalshiMarket> Markets { get; set; } = new();
    [JsonProperty("cursor")]
    public string Cursor { get; set; } = string.Empty;
}

public class KalshiOhlc
{
    [JsonProperty("open_dollars")]
    public string OpenDollars { get; set; } = "0";
    [JsonProperty("high_dollars")]
    public string HighDollars { get; set; } = "0";
    [JsonProperty("low_dollars")]
    public string LowDollars { get; set; } = "0";
    [JsonProperty("close_dollars")]
    public string CloseDollars { get; set; } = "0";

    public double Close => double.TryParse(CloseDollars, out var v) ? v : 0;
}

public class KalshiCandlestick
{
    [JsonProperty("end_period_ts")]
    public long EndPeriodTs { get; set; }
    [JsonProperty("yes_bid")]
    public KalshiOhlc YesBid { get; set; } = new();
    [JsonProperty("yes_ask")]
    public KalshiOhlc YesAsk { get; set; } = new();
    [JsonProperty("price")]
    public KalshiOhlc Price { get; set; } = new();
    [JsonProperty("volume_fp")]
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
    [JsonProperty("candlesticks")]
    public List<KalshiCandlestick> Candlesticks { get; set; } = new();
    [JsonProperty("ticker")]
    public string Ticker { get; set; } = string.Empty;
}

public class KalshiCutoffResponse
{
    [JsonProperty("market_settled_ts")]
    public DateTime? MarketSettledTs { get; set; }
    [JsonProperty("trades_created_ts")]
    public DateTime? TradesCreatedTs { get; set; }
    [JsonProperty("orders_updated_ts")]
    public DateTime? OrdersUpdatedTs { get; set; }
}
