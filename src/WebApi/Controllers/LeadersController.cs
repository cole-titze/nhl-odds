using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using WebApi.BusinessLogic.StatLeaders;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class LeadersController(IStatLeadersGetter statLeadersGetter)
{
    // The first season with box scores
    public const int FirstSeason = 2009;

    // The Leaders page (#182): the season's top 50 in one stat (StatLeaders.Stats); 400 for an unknown stat or
    // season. Ranked from the season's cached totals, so each stat costs one sort
    [HttpGet]
    public async Task<IResult> GetStatLeaders(string stat, int seasonStartYear)
    {
        if (seasonStartYear < FirstSeason || seasonStartYear > NhlSeason.Current(DateTime.UtcNow))
            return Results.BadRequest("Unknown season.");
        var result = await statLeadersGetter.GetStatLeaders(stat, seasonStartYear);
        return result is null
            ? Results.BadRequest($"Unknown stat. Use one of: {string.Join(", ", StatLeaders.Stats.Select(s => s.Key))}.")
            : Results.Ok(result);
    }
}
