using Newtonsoft.Json;

namespace Entities.ServiceModels.OddsApi;

public class OddsApiResponse
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("sport_key")]
    public string SportKey { get; set; } = string.Empty;
    [JsonProperty("sport_title")]
    public string SportTitle { get; set; } = string.Empty;
    [JsonProperty("commence_time")]
    public DateTime CommenceTime { get; set; }
    [JsonProperty("home_team")]
    public string HomeTeam { get; set; } = string.Empty;
    [JsonProperty("away_team")]
    public string AwayTeam { get; set; } = string.Empty;
    [JsonProperty("bookmakers")]
    public List<OddsApiBookmaker> Bookmakers { get; set; } = new();
}

public class OddsApiBookmaker
{
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty("last_update")]
    public DateTime LastUpdate { get; set; }
    [JsonProperty("markets")]
    public List<OddsApiMarket> Markets { get; set; } = new();
}

public class OddsApiMarket
{
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;
    [JsonProperty("last_update")]
    public DateTime LastUpdate { get; set; }
    [JsonProperty("outcomes")]
    public List<OddsApiOutcome> Outcomes { get; set; } = new();
}

public class OddsApiOutcome
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    [JsonProperty("price")]
    public int Price { get; set; }
}
