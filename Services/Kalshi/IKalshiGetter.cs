using Entities.ServiceModels.Kalshi;

namespace Services.Kalshi;

public interface IKalshiGetter
{
    Task<List<KalshiMarket>> GetOpenMarkets(string seriesTicker);
}
