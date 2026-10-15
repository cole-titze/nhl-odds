using DatabaseAccess.WebAdminRepository;
using Entities.ViewModels;
using WebApi.Mappers;

namespace WebApi.BusinessLogic.AdminService;

public class AdminService : IAdminService
{
    private readonly IWebAdminRepository _adminRepository;

    public AdminService(IWebAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<List<ErrorLogVM>> GetErrorLogs(int? seasonStartYear)
    {
        var errorLogs = await _adminRepository.GetErrorLogs(seasonStartYear);
        return errorLogs.Select(ErrorLogToErrorLogVmMapper.Map).ToList();
    }

    public async Task<List<SeasonHealthCheckVM>> GetHealthChecks()
    {
        var healthChecks = await _adminRepository.GetHealthChecks();
        return healthChecks.Select(SeasonHealthCheckToVmMapper.Map).ToList();
    }

    public async Task<List<LoadTestRunVM>> GetLoadTestRuns()
    {
        var runs = await _adminRepository.GetLoadTestRuns();
        return runs.Select(LoadTestRunToLoadTestRunVmMapper.Map).ToList();
    }

    /// <summary>The last <paramref name="days"/> days through <paramref name="today"/>; days with no probes are left out.</summary>
    public async Task<List<UptimeDayVM>> GetUptimeDays(int days, DateOnly today)
    {
        var uptimeDays = await _adminRepository.GetUptimeDays(today.AddDays(1 - days));
        return uptimeDays.Select(UptimeDayToUptimeDayVmMapper.Map).ToList();
    }
}