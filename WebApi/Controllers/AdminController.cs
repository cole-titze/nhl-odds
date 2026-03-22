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
    public async Task<IResult> GetErrorLogs(int? seasonStartYear)
    {
        var query = _db.ErrorLog.AsQueryable();

        if (seasonStartYear.HasValue)
            query = query.Where(e => e.SeasonStartYear == seasonStartYear.Value);

        var errors = await query
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
    public async Task<IResult> GetHealthChecks()
    {
        // Pull lightweight data into memory for cross-table checks
        var games = await _db.GameRaw
            .Select(g => new { g.Id, g.SeasonStartYear, g.HasBeenPlayed, g.GameDateUTC })
            .ToListAsync();

        var gameOddsGameIds = new HashSet<int>(
            await _db.GameOdds.Select(go => go.GameId).Distinct().ToListAsync());
        var liveBookmakerOddsGameIds = new HashSet<int>(
            await _db.BookmakerOdds
                .Join(_db.GameRaw, bo => bo.GameId, g => g.Id, (bo, g) => new { bo.GameId, bo.MarketLastUpdate, g.GameDateUTC })
                .Where(x => x.MarketLastUpdate > x.GameDateUTC)
                .Select(x => x.GameId)
                .Distinct()
                .ToListAsync());
        var bookmakerGameIds = new HashSet<int>(
            await _db.BookmakerOdds.Select(bo => bo.GameId).Distinct().ToListAsync());
        var cleanedGameIds = new HashSet<int>(
            await _db.GameCleaned.Select(gc => gc.GameId).Distinct().ToListAsync());

        // QueryDateUTC in UTC maps to the game date
        // (old: 10pm Central night before, new: 6am Central game day — both .Date = game date)
        var oddsFetchDateRaws = await _db.BookmakerOddsResponse
            .Where(r => r.QueryDateUTC != null)
            .Select(r => r.QueryDateUTC!.Value)
            .ToListAsync();
        var oddsFetchDateSet = new HashSet<DateTime>(oddsFetchDateRaws.Select(d => d.Date));

        var errorCounts = await _db.ErrorLog
            .GroupBy(e => e.SeasonStartYear)
            .Select(g => new { SeasonStartYear = g.Key, Count = g.Count() })
            .ToListAsync();
        var errorCountDict = errorCounts
            .Where(e => e.SeasonStartYear.HasValue)
            .ToDictionary(e => e.SeasonStartYear!.Value, e => e.Count);

        var checks = games
            .GroupBy(g => g.SeasonStartYear)
            .Select(g =>
            {
                var today = DateTime.UtcNow.Date;
                var allGames = g.ToList();
                var playedBeforeToday = allGames.Where(x => x.HasBeenPlayed && x.GameDateUTC.Date < today).ToList();
                var playedThroughToday = allGames.Where(x => x.HasBeenPlayed && x.GameDateUTC.Date <= today).ToList();
                var gameDates = playedBeforeToday.Select(x => x.GameDateUTC.Date).Distinct().ToList();
                return new SeasonHealthCheckVM
                {
                    SeasonStartYear = g.Key,
                    TotalGames = allGames.Count,
                    PlayedGames = playedThroughToday.Count,
                    MissingPredictions = allGames.Count(x => !gameOddsGameIds.Contains(x.Id)),
                    MissingBookmakerOdds = playedThroughToday.Count(x => !bookmakerGameIds.Contains(x.Id)),
                    MissingGameCleaned = allGames.Count(x => !cleanedGameIds.Contains(x.Id)),
                    MissingOddsFetchDays = gameDates.Count(d => !oddsFetchDateSet.Contains(d)),
                    LiveBookmakerOdds = allGames.Count(x => liveBookmakerOddsGameIds.Contains(x.Id)),
                    ErrorCount = errorCountDict.GetValueOrDefault(g.Key, 0),
                };
            })
            .OrderByDescending(c => c.SeasonStartYear)
            .ToList();

        return Results.Ok(checks);
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
