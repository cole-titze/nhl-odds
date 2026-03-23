using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class SeasonHealthCheckToVmMapper
{
    public static SeasonHealthCheckVM Map(SeasonHealthCheck healthCheck)
    {
        return new SeasonHealthCheckVM
        {
            SeasonStartYear = healthCheck.SeasonStartYear,
            TotalGames = healthCheck.TotalGames,
            PlayedGames = healthCheck.PlayedGames,
            MissingPredictions = healthCheck.MissingPredictions,
            MissingBookmakerOdds = healthCheck.MissingBookmakerOdds,
            MissingGameCleaned = healthCheck.MissingGameCleaned,
            MissingOddsFetchDays = healthCheck.MissingOddsFetchDays,
            LiveBookmakerOdds = healthCheck.LiveBookmakerOdds,
            ErrorCount = healthCheck.ErrorCount,
        };
    }
}
