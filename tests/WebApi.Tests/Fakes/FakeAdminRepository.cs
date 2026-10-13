using DatabaseAccess.WebAdminRepository;
using Entities.Models.Web;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeAdminRepository : IWebAdminRepository
{
    private readonly List<ErrorLog> _errorLogs;
    private readonly List<SeasonHealthCheck> _healthChecks;
    private readonly List<LoadTestRun> _loadTestRuns;

    public FakeAdminRepository(List<ErrorLog>? errorLogs = null, List<SeasonHealthCheck>? healthChecks = null,
        List<LoadTestRun>? loadTestRuns = null)
    {
        _errorLogs = errorLogs ?? new List<ErrorLog>();
        _healthChecks = healthChecks ?? new List<SeasonHealthCheck>();
        _loadTestRuns = loadTestRuns ?? new List<LoadTestRun>();
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

    public Task<List<LoadTestRun>> GetLoadTestRuns()
    {
        return Task.FromResult(_loadTestRuns.ToList());
    }
}