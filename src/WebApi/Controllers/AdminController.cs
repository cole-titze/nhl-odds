using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;
using WebApi.BusinessLogic.AdminService;
using WebApi.BusinessLogic.JobService;
using WebApi.Filters;
using WebApi.Mappers;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[RequireAdminKey]
public class AdminController
{
    private readonly IJobService _jobService;
    private readonly IAdminService _adminService;

    private const string DataCollectionJob = "data-collection";
    private const string PredictionJob = "prediction";
    private const string OddsFetchJob = "odds-fetch";
    private const string OddsBackfillJob = "odds-backfill";
    private const string PredictionBackfillJob = "prediction-backfill";
    private const string NextDayKalshiJob = "next-day-kalshi";
    private const string KalshiBackfillJob = "kalshi-backfill";
    // Load tests are one job per preset; scheduler/k8s/load-test.js defines what each preset runs
    private const string LoadTestJobPrefix = "load-test-";
    private static readonly string[] LoadTestPresets = ["light", "heavy", "stress", "max"];

    public AdminController(IJobService jobService, IAdminService adminService)
    {
        _jobService = jobService;
        _adminService = adminService;
    }

    [HttpPost]
    public IResult StartDataCollection()
    {
        if (CompletedToday(DataCollectionJob))
            return Results.Conflict(new { message = "Data collection already completed today." });

        if (!_jobService.RequestJob(DataCollectionJob))
            return Results.Conflict(new { message = "Data collection is already running or requested." });

        return Results.Ok(new { message = "Data collection requested." });
    }

    [HttpPost]
    public IResult StartPrediction()
    {
        if (CompletedToday(PredictionJob))
            return Results.Conflict(new { message = "Prediction already completed today." });

        if (!_jobService.RequestJob(PredictionJob))
            return Results.Conflict(new { message = "Prediction is already running or requested." });

        return Results.Ok(new { message = "Prediction requested." });
    }

    [HttpPost]
    public IResult StartOddsBackfill()
    {
        if (CompletedToday(OddsBackfillJob))
            return Results.Conflict(new { message = "Odds backfill already completed today." });

        if (!_jobService.RequestJob(OddsBackfillJob))
            return Results.Conflict(new { message = "Odds backfill is already running or requested." });

        return Results.Ok(new { message = "Odds backfill requested." });
    }

    [HttpPost]
    public IResult StartPredictionBackfill()
    {
        if (CompletedToday(PredictionBackfillJob))
            return Results.Conflict(new { message = "Prediction backfill already completed today." });

        if (!_jobService.RequestJob(PredictionBackfillJob))
            return Results.Conflict(new { message = "Prediction backfill is already running or requested." });

        return Results.Ok(new { message = "Prediction backfill requested." });
    }

    [HttpPost]
    public IResult StartKalshiBackfill()
    {
        if (CompletedToday(KalshiBackfillJob))
            return Results.Conflict(new { message = "Kalshi backfill already completed today." });

        if (!_jobService.RequestJob(KalshiBackfillJob))
            return Results.Conflict(new { message = "Kalshi backfill is already running or requested." });

        return Results.Ok(new { message = "Kalshi backfill requested." });
    }

    [HttpPost]
    public IResult StartLoadTest(string preset)
    {
        if (!LoadTestPresets.Contains(preset))
            return Results.BadRequest(new { message = $"Unknown load test preset '{preset}'." });

        // A load test next to another job would slow that job and skew the results
        var busyJob = _jobService.GetAllStatuses().FirstOrDefault(j => j.Status is "running" or "requested");
        if (busyJob != null)
            return Results.Conflict(new { message = $"Wait for {busyJob.Name} to finish before starting a load test." });

        if (!_jobService.RequestJob(LoadTestJobPrefix + preset))
            return Results.Conflict(new { message = "That load test is already running or requested." });

        return Results.Ok(new { message = "Load test requested." });
    }

    private static readonly TimeZoneInfo CentralTime = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private bool CompletedToday(string jobName)
    {
        var status = _jobService.GetStatus(jobName);
        return status.Status == "completed"
            && status.FinishedAt.HasValue
            && TimeZoneInfo.ConvertTimeFromUtc(status.FinishedAt.Value, CentralTime).Date
                == TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTime).Date;
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
    public async Task<IResult> GetLoadTestRuns()
    {
        var runs = await _adminService.GetLoadTestRuns();
        return Results.Ok(runs);
    }

    // Days are Central Time, like the rollup (scheduler/k8s/save-uptime.sh)
    [HttpGet]
    public async Task<IResult> GetUptimeDays(int days = 30)
    {
        if (days is < 1 or > 3660)
            return Results.BadRequest(new { message = "days must be between 1 and 3660." });

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTime));
        var uptimeDays = await _adminService.GetUptimeDays(days, today);
        return Results.Ok(uptimeDays);
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
            nextDayKalshi = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(NextDayKalshiJob)),
            kalshiBackfill = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(KalshiBackfillJob)),
            loadTestLight = LoadTestStatus("light"),
            loadTestHeavy = LoadTestStatus("heavy"),
            loadTestStress = LoadTestStatus("stress"),
            loadTestMax = LoadTestStatus("max"),
        };
        return Results.Ok(statuses);
    }

    // Load tests can run any number of times a day, so they never show "Completed Today"
    private JobInfoVM LoadTestStatus(string preset)
    {
        var vm = JobInfoToJobInfoVmMapper.Map(_jobService.GetStatus(LoadTestJobPrefix + preset));
        vm.CompletedToday = false;
        return vm;
    }
}