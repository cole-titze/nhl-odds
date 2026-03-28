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

    private const string DataCollectionJob = "data-collection";
    private const string PredictionJob = "prediction";
    private const string OddsFetchJob = "odds-fetch";
    private const string OddsBackfillJob = "odds-backfill";
    private const string PredictionBackfillJob = "prediction-backfill";
    private const string KalshiFetchJob = "kalshi-fetch";
    private const string KalshiBackfillJob = "kalshi-backfill";

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
