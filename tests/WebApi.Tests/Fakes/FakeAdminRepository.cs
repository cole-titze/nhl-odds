using DatabaseAccess.WebAdminRepository;
using Entities.Models.Web;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeAdminRepository : IWebAdminRepository
{
    private readonly List<ErrorLog> _errorLogs;
    private readonly List<SeasonHealthCheck> _healthChecks;
    private readonly List<LoadTestRun> _loadTestRuns;
    private readonly List<UptimeDay> _uptimeDays;

    public FakeAdminRepository(List<ErrorLog>? errorLogs = null, List<SeasonHealthCheck>? healthChecks = null,
        List<LoadTestRun>? loadTestRuns = null, List<UptimeDay>? uptimeDays = null)
    {
        _errorLogs = errorLogs ?? new List<ErrorLog>();
        _healthChecks = healthChecks ?? new List<SeasonHealthCheck>();
        _loadTestRuns = loadTestRuns ?? new List<LoadTestRun>();
        _uptimeDays = uptimeDays ?? new List<UptimeDay>();
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

    public Task<List<UptimeDay>> GetUptimeDays(DateOnly since)
    {
        return Task.FromResult(_uptimeDays.Where(d => d.Day >= since).OrderBy(d => d.Day).ToList());
    }
}