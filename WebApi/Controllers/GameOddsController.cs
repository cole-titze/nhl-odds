using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Mappers;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class GameOddsController
{
    private readonly ILogger<GameOddsController> _logger;
    private readonly IGameOddsGetter _gameOddsGetter;

    public GameOddsController(ILogger<GameOddsController> logger, IGameOddsGetter predictedGameBL)
    {
        _logger = logger;
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
        var predictedGames = await _gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear);
        var predictedGamesVM = GameOddsToViewModelsMapper.Map(predictedGames);
        return Results.Ok(predictedGamesVM);
    }
}
