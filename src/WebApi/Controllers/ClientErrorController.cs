using DatabaseAccess.WebErrorLogRepository;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using WebApi.BusinessLogic.ClientErrors;
using WebApi.Mappers;

namespace WebApi.Controllers;

/// <summary>
/// Errors from visitors' browsers (frontend/src/utils/errorReporting.ts), saved to ErrorLog with Source
/// <c>Frontend</c>. Each visitor has a small rate-limit bucket (the "api" policy in Program.cs) and
/// <see cref="ClientErrorThrottle"/> caps all visitors together (rate, daily cap, repeats); reports over it are dropped.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ClientErrorController(IWebErrorLogRepository errorRepository, ClientErrorThrottle throttle, ILogger<ClientErrorController> logger)
{
    private static readonly Counter Reports = Metrics.CreateCounter(
        "client_error_reports_total", "Errors reported by visitors' browsers", "result");

    [HttpPost]
    [RequestSizeLimit(ClientErrorLimits.MaxRequestBytes)]
    public async Task<IResult> Post(ClientErrorReport report, [FromHeader(Name = "User-Agent")] string? userAgent, CancellationToken cancellationToken)
    {
        if (!ClientErrorLimits.Kinds.Contains(report.Kind)
            || string.IsNullOrWhiteSpace(report.Message)
            || report.Page is { } page && !page.StartsWith('/'))
        {
            Reports.WithLabels("invalid").Inc();
            return Results.BadRequest();
        }

        // 204 either way: the browser has nothing to do about a dropped report
        var error = ClientErrorReportToErrorLogMapper.Map(report, userAgent, DateTime.UtcNow);
        // From the cut-short fields, so the keys held in memory stay small
        var decision = throttle.TryAcquire($"{report.Kind}|{error.ExceptionType}|{error.Message}");
        if (decision != ClientErrorThrottle.Decision.Allowed)
        {
            Reports.WithLabels(decision switch
            {
                ClientErrorThrottle.Decision.Duplicate => "duplicate",
                ClientErrorThrottle.Decision.OverDailyCap => "over_daily_cap",
                _ => "over_rate",
            }).Inc();
            return Results.NoContent();
        }

        try
        {
            await errorRepository.AddError(error, cancellationToken);
            Reports.WithLabels("saved").Inc();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Never a 500: the report is best effort and the page has moved on
            logger.LogWarning(e, "Couldn't save a client error report");
            Reports.WithLabels("failed").Inc();
        }
        return Results.NoContent();
    }
}
