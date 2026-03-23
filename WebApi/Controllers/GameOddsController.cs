using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using WebApi.BusinessLogic.GameOddsGetter;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class GameOddsController
{
    private readonly IGameOddsGetter _gameOddsGetter;

    public GameOddsController(IGameOddsGetter predictedGameBL)
    {
        _gameOddsGetter = predictedGameBL;
    }

    [HttpGet]
    public async Task<IResult> GetGameOddsInDateRange(DateTime startDate, DateTime endDate, int seasonStartYear)
    {
        var dateRange = new DateRange
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date
        };
        var predictedGamesVM = await _gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear);
        return Results.Ok(predictedGamesVM);
    }
}
