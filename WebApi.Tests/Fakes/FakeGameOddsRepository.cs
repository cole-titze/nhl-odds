using DatabaseAccess.WebGameOddsRepository;
using Entities.Models.Web;
using Entities.Types;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeGameOddsRepository : IGameOddsRepository
{
    private readonly IList<GameOdds> _gameOdds;

    public FakeGameOddsRepository(List<GameOdds> gameOdds)
    {
        _gameOdds = gameOdds;
    }

    public Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear)
    {
        var result = _gameOdds
            .Where(x => x.Game.GameDate.Date >= dateRange.StartDate && x.Game.GameDate.Date <= dateRange.EndDate)
            .ToList();
        return Task.FromResult<IEnumerable<GameOdds>>(result);
    }

    public Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear)
    {
        var result = _gameOdds
            .Where(x => x.Game.HomeTeam.Id == teamId || x.Game.AwayTeam.Id == teamId)
            .ToList();
        return Task.FromResult<IEnumerable<GameOdds>>(result);
    }

    public Task<List<GameOdds>> GetAllGameOddsForSeason(int seasonStartYear)
    {
        return Task.FromResult(_gameOdds.ToList());
    }
}
