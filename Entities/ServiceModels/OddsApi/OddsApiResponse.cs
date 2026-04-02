using System.Text.Json.Serialization;

namespace Entities.ServiceModels.OddsApi;

public class OddsApiResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("sport_key")]
    public string SportKey { get; set; } = string.Empty;
    [JsonPropertyName("sport_title")]
    public string SportTitle { get; set; } = string.Empty;
    [JsonPropertyName("commence_time")]
    public DateTime CommenceTime { get; set; }
    [JsonPropertyName("home_team")]
    public string HomeTeam { get; set; } = string.Empty;
    [JsonPropertyName("away_team")]
    public string AwayTeam { get; set; } = string.Empty;
    [JsonPropertyName("bookmakers")]
    public List<OddsApiBookmaker> Bookmakers { get; set; } = new();
}

public class OddsApiBookmaker
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("last_update")]
    public DateTime LastUpdate { get; set; }
    [JsonPropertyName("markets")]
    public List<OddsApiMarket> Markets { get; set; } = new();
}

public class OddsApiMarket
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    [JsonPropertyName("last_update")]
    public DateTime LastUpdate { get; set; }
    [JsonPropertyName("outcomes")]
    public List<OddsApiOutcome> Outcomes { get; set; } = new();
}

public class OddsApiOutcome
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("price")]
    public int Price { get; set; }
    [JsonPropertyName("point")]
    public double? Point { get; set; }
}
