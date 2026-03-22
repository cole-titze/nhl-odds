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
    public double MidPrice => (YesBid + YesAsk) / 2.0;
}

public class KalshiMarketsResponse
{
    [JsonProperty("markets")]
    public List<KalshiMarket> Markets { get; set; } = new();
    [JsonProperty("cursor")]
    public string Cursor { get; set; } = string.Empty;
}
