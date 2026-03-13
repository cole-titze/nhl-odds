using DatabaseAccess;
using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.BusinessLogic.JobService;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class AdminController
{
    private readonly IJobService _jobService;
    private readonly IConfiguration _configuration;
    private readonly GameDbContext _db;

    private const string DataCollectionJob = "data-collection";
    private const string PredictionJob = "prediction";

    public AdminController(IJobService jobService, IConfiguration configuration, GameDbContext db)
    {
        _jobService = jobService;
        _configuration = configuration;
        _db = db;
    }

    private string GetRepoRoot()
    {
        var configured = _configuration["AdminSettings:RepoRoot"];
        if (!string.IsNullOrEmpty(configured))
            return configured;

        // Walk up from current directory to find the solution file
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
        var repoRoot = GetRepoRoot();
        var started = _jobService.TryStart(
            DataCollectionJob,
            "dotnet",
            "run --project Entry --no-build",
            repoRoot);

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

    [HttpGet]
    public async Task<IResult> GetErrorLogs()
    {
        var errors = await _db.ErrorLog
            .OrderByDescending(e => e.TimestampUTC)
            .Take(50)
            .Select(e => new ErrorLogVM
            {
                Id = e.Id,
                TimestampUTC = e.TimestampUTC,
                GameId = e.GameId,
                SeasonStartYear = e.SeasonStartYear,
                ExceptionType = e.ExceptionType,
                Message = e.Message,
                StackTrace = e.StackTrace,
                Source = e.Source,
            })
            .ToListAsync();

        return Results.Ok(errors);
    }

    [HttpGet]
    public IResult GetJobStatuses()
    {
        var statuses = new
        {
            dataCollection = _jobService.GetStatus(DataCollectionJob),
            prediction = _jobService.GetStatus(PredictionJob),
        };
        return Results.Ok(statuses);
    }
}
