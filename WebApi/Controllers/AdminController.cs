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
    private const string OddsFetchJob = "odds-fetch";
    private const string OddsBackfillJob = "odds-backfill";
    private const string PredictionBackfillJob = "prediction-backfill";
    private const string KalshiFetchJob = "kalshi-fetch";

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
            "run --project Entry --no-build",
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
        var spreadGameIds = new HashSet<int>(
            await _db.GameSpreadTotalOdds.Where(st => st.ModelId == 2).Select(st => st.GameId).Distinct().ToListAsync());
        var totalGameIds = new HashSet<int>(
            await _db.GameSpreadTotalOdds.Where(st => st.ModelId == 3).Select(st => st.GameId).Distinct().ToListAsync());
        var liveBookmakerOddsGameIds = new HashSet<int>(
            await _db.BookmakerOdds
                .Join(_db.GameRaw, bo => bo.GameId, g => g.Id, (bo, g) => new { bo.GameId, bo.MarketLastUpdate, g.GameDateUTC })
                .Where(x => x.MarketLastUpdate > x.GameDateUTC)
                .Select(x => x.GameId)
                .Distinct()
                .ToListAsync());
        var bookmakerGameIds = new HashSet<int>(
            await _db.BookmakerOdds.Select(bo => bo.GameId).Distinct().ToListAsync());
        var spreadBookmakerGameIds = new HashSet<int>(
            await _db.BookmakerSpreads.Select(bs => bs.GameId).Distinct().ToListAsync());
        var totalBookmakerGameIds = new HashSet<int>(
            await _db.BookmakerTotals.Select(bt => bt.GameId).Distinct().ToListAsync());
        var cleanedGameIds = new HashSet<int>(
            await _db.GameCleaned.Select(gc => gc.GameId).Distinct().ToListAsync());

        // Convert fetch dates to Central time to match game dates
        var centralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var oddsFetchDateRaws = await _db.BookmakerOddsResponse
            .Where(r => r.QueryDateUTC != null)
            .Select(r => r.QueryDateUTC!.Value)
            .ToListAsync();
        var oddsFetchDateSet = new HashSet<DateTime>(
            oddsFetchDateRaws.Select(d => TimeZoneInfo.ConvertTimeFromUtc(d, centralZone).Date));

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
                // Use Central time dates to match the backfiller's fetch dates
                var gameDates = playedBeforeToday
                    .Select(x => TimeZoneInfo.ConvertTimeFromUtc(x.GameDateUTC, centralZone).Date)
                    .Distinct().ToList();
                return new SeasonHealthCheckVM
                {
                    SeasonStartYear = g.Key,
                    TotalGames = allGames.Count,
                    PlayedGames = playedThroughToday.Count,
                    MissingPredictions = g.Key <= 2009 ? -1
                        : allGames.Count(x => !gameOddsGameIds.Contains(x.Id))
                        + allGames.Count(x => !spreadGameIds.Contains(x.Id))
                        + allGames.Count(x => !totalGameIds.Contains(x.Id)),
                    MissingBookmakerOdds = g.Key < 2020 ? -1
                        : playedThroughToday.Count(x => !bookmakerGameIds.Contains(x.Id))
                        + playedThroughToday.Count(x => !spreadBookmakerGameIds.Contains(x.Id))
                        + playedThroughToday.Count(x => !totalBookmakerGameIds.Contains(x.Id)),
                    MissingGameCleaned = allGames.Count(x => !cleanedGameIds.Contains(x.Id)),
                    MissingOddsFetchDays = g.Key < 2020 ? -1
                        : gameDates.Count(d => !oddsFetchDateSet.Contains(d)),
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
            oddsFetch = _jobService.GetStatus(OddsFetchJob),
            prediction = _jobService.GetStatus(PredictionJob),
            oddsBackfill = _jobService.GetStatus(OddsBackfillJob),
            predictionBackfill = _jobService.GetStatus(PredictionBackfillJob),
            kalshiFetch = _jobService.GetStatus(KalshiFetchJob),
        };
        return Results.Ok(statuses);
    }
}
