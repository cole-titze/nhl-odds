namespace WebApi.BusinessLogic.JobService;

public class DailyOddsFetchService : BackgroundService
{
    private readonly IJobService _jobService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DailyOddsFetchService> _logger;
    private readonly TimeOnly _runTime = new(22, 0);

    public DailyOddsFetchService(IJobService jobService, IConfiguration configuration, ILogger<DailyOddsFetchService> logger)
    {
        _jobService = jobService;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            _logger.LogInformation("Next scheduled odds fetch in {Hours:F1} hours", delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunOddsFetch(stoppingToken);
        }
    }

    private async Task RunOddsFetch(CancellationToken stoppingToken)
    {
        var repoRoot = GetRepoRoot();

        _logger.LogInformation("Starting scheduled odds fetch");
        var started = _jobService.TryStart(
            "odds-fetch",
            "dotnet",
            "run --project Entry --no-build",
            repoRoot,
            new Dictionary<string, string> { { "RUN_MODE", "NextDayOdds" } });

        if (!started)
        {
            _logger.LogWarning("Scheduled odds fetch skipped — a job is already running");
            return;
        }

        await _jobService.WaitForCompletion("odds-fetch", stoppingToken);
    }

    private TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var next = now.Date.Add(_runTime.ToTimeSpan());
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }

    private string GetRepoRoot()
    {
        var configured = _configuration["AdminSettings:RepoRoot"];
        if (!string.IsNullOrEmpty(configured))
            return configured;

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
