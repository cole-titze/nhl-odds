namespace WebApi.BusinessLogic.JobService;

public class DailyDataCollectionService : BackgroundService
{
    private readonly IJobService _jobService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DailyDataCollectionService> _logger;
    private readonly TimeOnly _runTime = new(3, 0);

    public DailyDataCollectionService(IJobService jobService, IConfiguration configuration, ILogger<DailyDataCollectionService> logger)
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
            _logger.LogInformation("Next scheduled pipeline in {Hours:F1} hours", delay.TotalHours);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunPipeline(stoppingToken);
        }
    }

    private async Task RunPipeline(CancellationToken stoppingToken)
    {
        var repoRoot = GetRepoRoot();

        _logger.LogInformation("Starting scheduled data collection");
        var started = _jobService.TryStart(
            "data-collection",
            "dotnet",
            "run --project Entry --no-build",
            repoRoot);

        if (!started)
        {
            _logger.LogWarning("Scheduled data collection skipped — already running");
            return;
        }

        await _jobService.WaitForCompletion("data-collection", stoppingToken);

        var status = _jobService.GetStatus("data-collection");
        if (status.Status != "completed")
        {
            _logger.LogWarning("Data collection did not complete successfully — skipping prediction");
            return;
        }

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

    private TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var next = now.Date.Add(_runTime.ToTimeSpan());
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }

    private string GetPythonPath(string repoRoot)
    {
        var configured = _configuration["AdminSettings:PythonPath"];
        if (!string.IsNullOrEmpty(configured))
            return configured;

        var venvPython = Path.Combine(repoRoot, ".venv", "bin", "python3");
        return File.Exists(venvPython) ? venvPython : "python3";
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
