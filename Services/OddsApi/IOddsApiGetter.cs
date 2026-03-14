using Entities.ServiceModels.OddsApi;

namespace Services.OddsApi;

public interface IOddsApiGetter
{
    Task<OddsApiResult> GetUpcomingOdds();
    int? GetRemainingRequests();
}

public class OddsApiResult
{
    public List<OddsApiResponse> Responses { get; set; } = new();
    public string RawJson { get; set; } = string.Empty;
}
