using Entities.Models.Web;

namespace DatabaseAccess.WebAdminRepository;

public interface IWebAdminRepository
{
    Task<List<ErrorLog>> GetErrorLogs(int? seasonStartYear);
    Task<List<SeasonHealthCheck>> GetHealthChecks();
    Task<List<LoadTestRun>> GetLoadTestRuns();
    Task<List<UptimeDay>> GetUptimeDays(DateOnly since);
}