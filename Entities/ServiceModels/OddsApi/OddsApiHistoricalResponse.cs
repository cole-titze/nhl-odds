using Newtonsoft.Json;

namespace Entities.ServiceModels.OddsApi;

public class OddsApiHistoricalResponse
{
    [JsonProperty("timestamp")]
    public DateTime? Timestamp { get; set; }
    [JsonProperty("previous_timestamp")]
    public DateTime? PreviousTimestamp { get; set; }
    [JsonProperty("next_timestamp")]
    public DateTime? NextTimestamp { get; set; }
    [JsonProperty("data")]
    public List<OddsApiResponse> Data { get; set; } = new();
}
