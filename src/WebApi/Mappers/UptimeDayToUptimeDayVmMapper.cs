using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class UptimeDayToUptimeDayVmMapper
{
    public static UptimeDayVM Map(UptimeDay day)
    {
        return new UptimeDayVM
        {
            Day = day.Day,
            ExpectedProbes = day.ExpectedProbes,
            Probes = day.Probes,
            SuccessfulProbes = day.SuccessfulProbes,
            AvgResponseMs = day.AvgResponseMs,
        };
    }
}
