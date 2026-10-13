using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class LoadTestRunToLoadTestRunVmMapper
{
    public static LoadTestRunVM Map(LoadTestRun run)
    {
        return new LoadTestRunVM
        {
            Id = run.Id,
            Preset = run.Preset,
            Target = run.Target,
            MaxVus = run.MaxVus,
            RampUpSeconds = run.RampUpSeconds,
            HoldSeconds = run.HoldSeconds,
            RampDownSeconds = run.RampDownSeconds,
            ThinkTimeMinSeconds = run.ThinkTimeMinSeconds,
            ThinkTimeMaxSeconds = run.ThinkTimeMaxSeconds,
            PageMix = run.PageMix,
            StartedUTC = run.StartedUTC,
            FinishedUTC = run.FinishedUTC,
            Requests = run.Requests,
            RequestsPerSecond = run.RequestsPerSecond,
            P50Ms = run.P50Ms,
            P95Ms = run.P95Ms,
            P99Ms = run.P99Ms,
            MaxMs = run.MaxMs,
            Http5xx = run.Http5xx,
            Http429 = run.Http429,
            OtherErrors = run.OtherErrors,
        };
    }
}