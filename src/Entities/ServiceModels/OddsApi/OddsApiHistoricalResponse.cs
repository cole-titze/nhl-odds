using System.Text.Json.Serialization;

namespace Entities.ServiceModels.OddsApi;

public class OddsApiHistoricalResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }
    [JsonPropertyName("previous_timestamp")]
    public DateTime? PreviousTimestamp { get; set; }
    [JsonPropertyName("next_timestamp")]
    public DateTime? NextTimestamp { get; set; }
    [JsonPropertyName("data")]
    public List<OddsApiResponse> Data { get; set; } = new();
}
