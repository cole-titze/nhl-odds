using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.TrackRecord;
using WebApi.Caching;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class TrackRecordController(ITrackRecordGetter getter, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
{
    // liveOnly: only predictions saved before puck drop, leaving out walk-forward backfill rows
    [HttpGet]
    public async Task<IResult> GetTrackRecord(int seasonStartYear, bool liveOnly = false)
    {
        var result = await cache.GetOrSetAsync(ApiCache.TrackRecordKey(seasonStartYear, liveOnly),
            () => getter.GetTrackRecord(seasonStartYear, liveOnly),
            r => 1 + r.Calibration.Count + r.LogLossByDate.Count + r.Sources.Count, ApiCache.DateRangeLifetime);
        return Results.Ok(result);
    }
}
