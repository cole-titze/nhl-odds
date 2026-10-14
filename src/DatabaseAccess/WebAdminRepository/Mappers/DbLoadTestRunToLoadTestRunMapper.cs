using Entities.DbModels;
using Entities.Models.Web;

namespace DatabaseAccess.WebAdminRepository.Mappers;

public static class DbLoadTestRunToLoadTestRunMapper
{
    public static LoadTestRun Map(DbLoadTestRun db, IEnumerable<DbLoadTestRunPage> pages)
    {
        return new LoadTestRun
        {
            Id = db.Id,
            Preset = db.Preset,
            Target = db.Target,
            MaxVus = db.MaxVus,
            RampUpSeconds = db.RampUpSeconds,
            HoldSeconds = db.HoldSeconds,
            RampDownSeconds = db.RampDownSeconds,
            ThinkTimeMinSeconds = db.ThinkTimeMinSeconds,
            ThinkTimeMaxSeconds = db.ThinkTimeMaxSeconds,
            PageMix = db.PageMix,
            StartedUTC = db.StartedUTC,
            FinishedUTC = db.FinishedUTC,
            Requests = db.Requests,
            RequestsPerSecond = db.RequestsPerSecond,
            P50Ms = db.P50Ms,
            P95Ms = db.P95Ms,
            P99Ms = db.P99Ms,
            MaxMs = db.MaxMs,
            Http5xx = db.Http5xx,
            Http429 = db.Http429,
            OtherErrors = db.OtherErrors,
            Pages = pages.Select(p => new LoadTestRunPage
            {
                Page = p.Page,
                Requests = p.Requests,
                RequestsPerSecond = p.RequestsPerSecond,
                P50Ms = p.P50Ms,
                P95Ms = p.P95Ms,
                P99Ms = p.P99Ms,
                MaxMs = p.MaxMs,
                Http5xx = p.Http5xx,
                Http429 = p.Http429,
                OtherErrors = p.OtherErrors,
            }).ToList(),
        };
    }
}