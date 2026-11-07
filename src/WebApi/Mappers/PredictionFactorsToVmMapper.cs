using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class PredictionFactorsToVmMapper
{
    public static PredictionFactorsVM Map(PredictionFactors factors)
    {
        return new PredictionFactorsVM
        {
            GameId = factors.GameId,
            RunDateUTC = factors.RunDateUTC,
            BaseHomeOdds = factors.BaseHomeOdds,
            Factors = factors.Factors.Select(f => new PredictionFactorVM
            {
                Factor = f.Factor,
                Detail = f.Detail,
                Contribution = f.Contribution,
            }).ToList(),
        };
    }
}
