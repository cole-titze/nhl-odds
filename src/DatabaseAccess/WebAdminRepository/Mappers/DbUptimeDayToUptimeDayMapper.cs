using Entities.DbModels;
using Entities.Models.Web;

namespace DatabaseAccess.WebAdminRepository.Mappers;

public static class DbUptimeDayToUptimeDayMapper
{
    public static UptimeDay Map(DbUptimeDay db)
    {
        return new UptimeDay
        {
            Day = db.Day,
            ExpectedProbes = db.ExpectedProbes,
            Probes = db.Probes,
            SuccessfulProbes = db.SuccessfulProbes,
            AvgResponseMs = db.AvgResponseMs,
        };
    }
}
