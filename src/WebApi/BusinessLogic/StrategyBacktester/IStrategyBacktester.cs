using Entities.ViewModels;

namespace WebApi.BusinessLogic.StrategyBacktester;

public interface IStrategyBacktester
{
    Task<IEnumerable<BestStrategyVM>> GetBestStrategies(int seasonStartYear);
    Task<IEnumerable<StrategySeasonResultsVM>> GetSeasonResults(int seasonStartYear);
}
