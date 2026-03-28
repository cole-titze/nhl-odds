using Microsoft.AspNetCore.Mvc;
using WebApi.BusinessLogic.AdminService;
using WebApi.BusinessLogic.JobService;
using WebApi.Mappers;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class AdminController
{
    private readonly IJobService _jobService;
    private readonly IAdminService _adminService;
    private readonly IConfiguration _configuration;

    private const string DataCollectionJob = "data-collection";
    private const string PredictionJob = "prediction";
    private const string OddsFetchJob = "odds-fetch";
    private const string OddsBackfillJob = "odds-backfill";
    private const string PredictionBackfillJob = "prediction-backfill";
    private const string KalshiFetchJob = "kalshi-fetch";
    private const string KalshiBackfillJob = "kalshi-backfill";

    public AdminController(IJobService jobService, IAdminService adminService, IConfiguration configuration)
    {
        _jobService = jobService;
        _adminService = adminService;
        _configuration = configuration;
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

    [HttpPost]
    public IResult StartDataCollection()
    {
        if (CompletedToday(DataCollectionJob))
            return Results.Conflict(new { message = "Data collection already completed today." });

        var repoRoot = GetRepoRoot();
        var started = _jobService.TryStart(
            DataCollectionJob,
            "dotnet",
            "run --project Entry --no-build -c Release",
            repoRoot,
            new Dictionary<string, string> { { "RUN_MODE", "NhlAdd" } });

        if (!started)
            return Results.Conflict(new { message = "Data collection is already running." });

        return Results.Ok(new { message = "Data collection started." });
    }

    [HttpPost]
    public IResult StartPrediction()
    {
        var repoRoot = GetRepoRoot();
        var defaultPython = Path.Combine(repoRoot, ".venv", "bin", "python3");
        var pythonPath = _configuration["AdminSettings:PythonPath"]
            ?? (File.Exists(defaultPython) ? defaultPython : "python3");
        var started = _jobService.TryStart(
            PredictionJob,
            pythonPath,
            "-m game_predictor --mode predict",
            repoRoot);

        if (!started)
            return Results.Conflict(new { message = "Prediction is already running." });

        return Results.Ok(new { message = "Prediction started." });
    }

    [HttpPost]
    public IResult StartOddsBackfill()
    {
        if (CompletedToday(OddsBackfillJob))
            return Results.Conflict(new { message = "Odds backfill already completed today." });

        var repoRoot = GetRepoRoot();
        var started = _jobService.TryStart(
            OddsBackfillJob,
            "dotnet",
            "run --project Entry --no-build -c Release",
            repoRoot,
            new Dictionary<string, string> { { "RUN_MODE", "BackfillOdds" } });

        if (!started)
            return Results.Conflict(new { message = "Odds backfill is already running." });

        return Results.Ok(new { message = "Odds backfill started." });
    }

    [HttpPost]
    public IResult StartPredictionBackfill()
    {
        if (CompletedToday(PredictionBackfillJob))
            return Results.Conflict(new { message = "Prediction backfill already completed today." });

        var repoRoot = GetRepoRoot();
        var defaultPython = Path.Combine(repoRoot, ".venv", "bin", "python3");
        var pythonPath = _configuration["AdminSettings:PythonPath"]
            ?? (File.Exists(defaultPython) ? defaultPython : "python3");
        var started = _jobService.TryStart(
            PredictionBackfillJob,
            pythonPath,
            "-m game_predictor --mode backfill",
            repoRoot);

        if (!started)
            return Results.Conflict(new { message = "Prediction backfill is already running." });

        return Results.Ok(new { message = "Prediction backfill started." });
    }

    [HttpPost]
    public IResult StartKalshiBackfill()
    {
        if (CompletedToday(KalshiBackfillJob))
            return Results.Conflict(new { message = "Kalshi backfill already completed today." });

        var repoRoot = GetRepoRoot();
        var started = _jobService.TryStart(
            KalshiBackfillJob,
            "dotnet",
            "run --project Entry --no-build -c Release",
            repoRoot,
            new Dictionary<string, string> { { "RUN_MODE", "BackfillKalshi" } });

        if (!started)
            return Results.Conflict(new { message = "Kalshi backfill is already running." });

        return Results.Ok(new { message = "Kalshi backfill started." });
    }

    private bool CompletedToday(string jobName)
    {
        var status = _jobService.GetStatus(jobName);
        return status.Status == "completed"
            && status.FinishedAt.HasValue
            && status.FinishedAt.Value.Date == DateTime.UtcNow.Date;
    }

    [HttpGet]
    public async Task<IResult> GetErrorLogs(int? seasonStartYear)
    {
        var errors = await _adminService.GetErrorLogs(seasonStartYear);
        return Results.Ok(errors);
    }

    [HttpGet]
    public async Task<IResult> GetHealthChecks()
    {
        var checks = await _adminService.GetHealthChecks();
        return Results.Ok(checks);
    }

    [HttpGet]
    public IResult GetJobStatuses()
    {
        var statuses = new
        {
            dataCollection = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(DataCollectionJob)),
            oddsFetch = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(OddsFetchJob)),
            prediction = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(PredictionJob)),
            oddsBackfill = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(OddsBackfillJob)),
            predictionBackfill = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(PredictionBackfillJob)),
            kalshiFetch = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(KalshiFetchJob)),
            kalshiBackfill = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(KalshiBackfillJob)),
        };
        return Results.Ok(statuses);
    }
}
