using Entities.ServiceModels.OddsApi;

namespace Services.OddsApi;

public interface IOddsApiGetter
{
    Task<OddsApiResult> GetUpcomingOdds();
    Task<OddsApiResult> GetHistoricalOdds(DateTime date);
    Task<OddsApiResult> GetOddsStartingBetween(string markets, DateTime fromUtc, DateTime toUtc);
    Task<int?> GetRemainingCredits();
    int? GetRemainingRequests();
}

public class OddsApiResult
{
    public List<OddsApiResponse> Responses { get; set; } = new();
    public string RawJson { get; set; } = string.Empty;
}

public class OddsApiRateLimitException : Exception
{
    public OddsApiRateLimitException(string message) : base(message) { }
}