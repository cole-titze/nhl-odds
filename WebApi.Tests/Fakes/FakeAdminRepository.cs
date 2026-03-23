using DatabaseAccess.WebAdminRepository;
using Entities.Models.Web;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeAdminRepository : IWebAdminRepository
{
    private readonly List<ErrorLog> _errorLogs;
    private readonly List<SeasonHealthCheck> _healthChecks;

    public FakeAdminRepository(List<ErrorLog>? errorLogs = null, List<SeasonHealthCheck>? healthChecks = null)
    {
        _errorLogs = errorLogs ?? new List<ErrorLog>();
        _healthChecks = healthChecks ?? new List<SeasonHealthCheck>();
    }

    public Task<List<ErrorLog>> GetErrorLogs(int? seasonStartYear)
    {
        var result = seasonStartYear.HasValue
            ? _errorLogs.Where(e => e.SeasonStartYear == seasonStartYear.Value).ToList()
            : _errorLogs;
        return Task.FromResult(result.ToList());
    }

    public Task<List<SeasonHealthCheck>> GetHealthChecks()
    {
        return Task.FromResult(_healthChecks.ToList());
    }
}
