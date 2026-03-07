using DatabaseAccess.WebGameOddsRepository;
using Entities.Models.Web;
using Entities.Types;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeGameOddsRepository : IGameOddsRepository
{
    private readonly IList<GameOdds> _predictedGames;
    public FakeGameOddsRepository(List<GameOdds> predictedGames)
    {
        _predictedGames = predictedGames;
    }
    public async Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear)
    {
        return await Task.FromResult(_predictedGames
            .Where(x => x.Game.GameDate.Date >= dateRange.StartDate && x.Game.GameDate.Date <= dateRange.EndDate)
            .ToList());
    }

    public Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear)
    {
        throw new NotImplementedException();
    }
}
