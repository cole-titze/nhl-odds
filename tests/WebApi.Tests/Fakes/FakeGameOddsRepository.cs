using DatabaseAccess.WebGameOddsRepository;
using Entities.Models.Web;
using Entities.Types;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeGameOddsRepository : IGameOddsRepository
{
    private readonly IList<GameOdds> _gameOdds;
    private readonly DateTime _now;

    public FakeGameOddsRepository(List<GameOdds> gameOdds, DateTime? now = null)
    {
        _gameOdds = gameOdds;
        _now = now ?? DateTime.UtcNow;
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

    public Task<List<SitemapGame>> GetSitemapGames()
    {
        var result = _gameOdds
            .OrderBy(x => x.Game.Id)
            .Select(x => new SitemapGame(x.Game.Id, x.Game.SeasonStartYear, x.Game.GameDate,
                x.Game.AwayTeam.Id, x.Game.AwayTeam.TeamName, x.Game.HomeTeam.Id, x.Game.HomeTeam.TeamName,
                x.Game.HasBeenPlayed))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<GameOdds?> GetGameOdds(int gameId)
    {
        return Task.FromResult(_gameOdds.FirstOrDefault(x => x.Game.Id == gameId));
    }

    public Task<List<GameOdds>> GetAllGameOddsForSeason(int seasonStartYear)
    {
        return Task.FromResult(_gameOdds.ToList());
    }

    public Task<DateTime?> GetAnchorDate(int seasonStartYear)
    {
        var nowCentralDate = _now.AddHours(-6).Date;

        var upcoming = _gameOdds
            .Where(x => x.Game.GameDate.AddHours(-6).Date >= nowCentralDate)
            .OrderBy(x => x.Game.GameDate)
            .Select(x => (DateTime?)x.Game.GameDate.AddHours(-6).Date)
            .FirstOrDefault();

        if (upcoming.HasValue)
            return Task.FromResult(upcoming);

        var past = _gameOdds
            .Where(x => x.Game.GameDate.AddHours(-6).Date < nowCentralDate)
            .OrderByDescending(x => x.Game.GameDate)
            .Select(x => (DateTime?)x.Game.GameDate.AddHours(-6).Date)
            .FirstOrDefault();

        return Task.FromResult(past);
    }
}
