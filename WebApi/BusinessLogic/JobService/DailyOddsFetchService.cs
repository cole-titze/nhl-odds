namespace WebApi.BusinessLogic.JobService;

public class DailyOddsFetchService : BackgroundService
{
    private readonly IJobService _jobService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DailyOddsFetchService> _logger;
    private readonly TimeOnly _runTime = new(6, 0);

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
            GetEntryArgs(repoRoot),
            repoRoot,
            new Dictionary<string, string> { { "RUN_MODE", "NextDayOdds" } });

        if (!started)
        {
            _logger.LogWarning("Scheduled odds fetch skipped — a job is already running");
            return;
        }

        await _jobService.WaitForCompletion("odds-fetch", stoppingToken);

        var status = _jobService.GetStatus("odds-fetch");
        if (status.Status != "completed")
            _logger.LogWarning("Odds fetch did not complete successfully — prediction will still run");

        // Fetch Kalshi odds (separate from The Odds API)
        _logger.LogInformation("Starting scheduled Kalshi fetch");
        var kalshiStarted = _jobService.TryStart(
            "kalshi-fetch",
            "dotnet",
            GetEntryArgs(repoRoot),
            repoRoot,
            new Dictionary<string, string> { { "RUN_MODE", "KalshiFetch" } });

        if (kalshiStarted)
            await _jobService.WaitForCompletion("kalshi-fetch", stoppingToken);
        else
            _logger.LogWarning("Scheduled Kalshi fetch skipped — already running");

        _logger.LogInformation("Starting scheduled prediction");
        var pythonPath = GetPythonPath(repoRoot);
        var predStarted = _jobService.TryStart(
            "prediction",
            pythonPath,
            "-m game_predictor --mode predict",
            repoRoot);

        if (!predStarted)
            _logger.LogWarning("Scheduled prediction skipped — already running");
    }

    private string GetPythonPath(string repoRoot)
    {
        var configured = _configuration["AdminSettings:PythonPath"];
        if (!string.IsNullOrEmpty(configured))
            return configured;

        var venvPython = Path.Combine(repoRoot, ".venv", "bin", "python3");
        return File.Exists(venvPython) ? venvPython : "python3";
    }

    private static string GetEntryArgs(string repoRoot)
    {
        var releaseBin = Path.Combine(repoRoot, "Entry", "bin", "Release");
        return Directory.Exists(releaseBin)
            ? "run --project Entry --no-build -c Release"
            : "run --project Entry";
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
