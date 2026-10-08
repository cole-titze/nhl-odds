using Entities.ServiceModels;

namespace Services.NhlData;

public interface INhlRosterReportGetter
{
    /// <summary>Current Active and Injured Reserve lists for every team, or empty if the report can't be fetched.</summary>
    Task<List<ServiceRosterReportEntry>> GetRosterReport();
}