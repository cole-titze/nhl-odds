using System.ComponentModel;
using Entities.Types;
using Entities.ViewModels;
using ModelContextProtocol.Server;
using WebApi.BusinessLogic.GameOddsGetter;

namespace WebApi.McpTools;

[McpServerToolType]
public class GameOddsMcpTools(IGameOddsGetter gameOddsGetter)
{
    [McpServerTool, Description("Get NHL game odds and model predictions for today.")]
    public async Task<IEnumerable<GameOddsVM>> GetTodaysGames(
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
    {
        var today = DateTime.UtcNow.Date;
        var dateRange = new DateRange { StartDate = today, EndDate = today };
        return await gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear);
    }

    [McpServerTool, Description("Get NHL game odds and model predictions for a date range.")]
    public async Task<IEnumerable<GameOddsVM>> GetGamesInDateRange(
        [Description("Start date (UTC)")] DateTime startDate,
        [Description("End date (UTC)")] DateTime endDate,
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
    {
        var dateRange = new DateRange { StartDate = startDate, EndDate = endDate };
        return await gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear);
    }
}
