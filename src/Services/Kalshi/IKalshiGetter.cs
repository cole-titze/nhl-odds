using Entities.ServiceModels.Kalshi;

namespace Services.Kalshi;

public interface IKalshiGetter
{
    Task<List<KalshiMarket>> GetOpenMarkets(string seriesTicker);
    Task<KalshiCutoffResponse?> GetCutoff();
    Task<List<KalshiMarket>> GetSettledMarkets(string seriesTicker);
    Task<List<KalshiMarket>> GetHistoricalMarkets(string seriesTicker);
    Task<KalshiCandlestickResponse?> GetCandlesticks(string seriesTicker, string ticker, long startTs, long endTs, int periodInterval);
}