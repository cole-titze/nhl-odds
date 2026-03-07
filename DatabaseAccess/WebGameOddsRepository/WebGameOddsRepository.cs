using DatabaseAccess.WebGameOddsRepository.Mappers;
using DatabaseAccess.WebTeamRepository.Mappers;
using Entities.DbModels;
using Entities.Models.Web;
using Entities.Types;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebGameOddsRepository;

public class GameOddsRepository : IGameOddsRepository
{
    private readonly GameDbContext _dbContext;
    private const int MAX_GAMES = 16;
    public GameOddsRepository(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear)
    {
        var seasonTeams = await GetSeasonTeams(seasonStartYear);

        var dbGameOdds = await _dbContext.GameOdds
            .Include(x => x.Game)
            .Where(x => x.Game != null
                && x.Game.GameDateUTC.AddHours(-6).Date >= dateRange.StartDate.Date
                && x.Game.GameDateUTC.AddHours(-6).Date <= dateRange.EndDate.Date)
            .OrderBy(x => x.Game!.GameDateUTC)
            .ToListAsync();

        var latestPerGame = GetLatestOddsPerGame(dbGameOdds);

        return DbGameOddsToGameOddsMapper.Map(latestPerGame, seasonTeams);
    }

    public async Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear)
    {
        var seasonTeams = await GetSeasonTeams(seasonStartYear);

        var dbGameOdds = await _dbContext.GameOdds
            .Include(x => x.Game)
            .Where(x => x.Game != null
                && (x.Game.AwayTeamId == teamId || x.Game.HomeTeamId == teamId)
                && x.Game.SeasonStartYear == seasonStartYear)
            .OrderByDescending(x => x.Game!.GameDateUTC)
            .ToListAsync();

        var latestPerGame = GetLatestOddsPerGame(dbGameOdds);

        return DbGameOddsToGameOddsMapper.Map(latestPerGame, seasonTeams);
    }

    private async Task<Dictionary<int, DbSeasonTeam>> GetSeasonTeams(int seasonStartYear)
    {
        var teams = await _dbContext.SeasonTeam
            .Where(x => x.SeasonStartYear == seasonStartYear)
            .ToListAsync();
        return teams.ToDictionary(t => t.TeamId);
    }

    private static List<DbGameOdds> GetLatestOddsPerGame(List<DbGameOdds> dbGameOdds)
    {
        return dbGameOdds
            .GroupBy(x => x.GameId)
            .Select(g => g.OrderByDescending(x => x.RunDateUTC).First())
            .ToList();
    }
}
