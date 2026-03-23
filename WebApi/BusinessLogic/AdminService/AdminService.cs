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
}
