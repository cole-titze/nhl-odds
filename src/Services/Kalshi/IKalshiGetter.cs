using Entities.ServiceModels.Kalshi;

namespace Services.Kalshi;

public interface IKalshiGetter
{
    Task<List<KalshiMarket>> GetOpenMarkets(string seriesTicker);
    Task<KalshiCutoffResponse?> GetCutoff();
    Task<List<KalshiMarket>> GetSettledMarkets(string seriesTicker);
    Task<List<KalshiMarket>> GetHistoricalMarkets(string seriesTicker);
    /// <summary>The event's markets, from the historical endpoint once Kalshi has moved them there</summary>
    Task<List<KalshiMarket>> GetEventMarkets(string eventTicker);
    Task<KalshiCandlestickResponse?> GetCandlesticks(string seriesTicker, string ticker, long startTs, long endTs, int periodInterval);
}