using Entities.ViewModels;

namespace WebApi.BusinessLogic.AdminService;

public interface IAdminService
{
    Task<List<ErrorLogVM>> GetErrorLogs(int? seasonStartYear);
    Task<List<SeasonHealthCheckVM>> GetHealthChecks();
    Task<List<LoadTestRunVM>> GetLoadTestRuns();
    Task<List<UptimeDayVM>> GetUptimeDays(int days, DateOnly today);
}