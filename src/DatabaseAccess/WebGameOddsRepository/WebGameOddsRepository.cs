using DatabaseAccess.WebBookmakerOddsRepository;
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
    private readonly IWebBookmakerOddsRepository _bookmakerOddsRepo;
    private const int MAX_GAMES = 16;
    private static readonly TimeZoneInfo CentralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    public GameOddsRepository(GameDbContext dbContext, IWebBookmakerOddsRepository bookmakerOddsRepo)
    {
        _dbContext = dbContext;
        _bookmakerOddsRepo = bookmakerOddsRepo;
    }

    private static double CentralOffsetHours(DateTime utcNow) =>
        CentralZone.GetUtcOffset(utcNow).TotalHours;

    public async Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear)
    {
        var seasonTeams = await GetSeasonTeams(seasonStartYear);
        var offset = CentralOffsetHours(DateTime.UtcNow);

        var games = await _dbContext.GameRaw
            .AsNoTracking()
            .Where(g => g.GameDateUTC.AddHours(offset).Date >= dateRange.StartDate.Date
                     && g.GameDateUTC.AddHours(offset).Date <= dateRange.EndDate.Date)
            .OrderBy(g => g.GameDateUTC)
            .ToListAsync();

        var latestByGame = await GetLatestOddsForGames(games);
        var gameOdds = DbGameOddsToGameOddsMapper.Map(games, latestByGame, seasonTeams);

        await AttachBookmakerOdds(gameOdds);
        await AttachSpreadTotalPredictions(gameOdds);
        return gameOdds;
    }

    public async Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear)
    {
        var seasonTeams = await GetSeasonTeams(seasonStartYear);

        var games = await _dbContext.GameRaw
            .AsNoTracking()
            .Where(g => (g.AwayTeamId == teamId || g.HomeTeamId == teamId)
                     && g.SeasonStartYear == seasonStartYear)
            .OrderByDescending(g => g.GameDateUTC)
            .ToListAsync();

        var latestByGame = await GetLatestOddsForGames(games);
        var gameOdds = DbGameOddsToGameOddsMapper.Map(games, latestByGame, seasonTeams);

        await AttachBookmakerOdds(gameOdds);
        await AttachSpreadTotalPredictions(gameOdds);
        return gameOdds;
    }

    private async Task<Dictionary<int, DbGameOdds>> GetLatestOddsForGames(IEnumerable<DbGameRaw> games)
    {
        var gameIds = games.Select(g => g.Id).ToList();
        if (gameIds.Count == 0)
            return new Dictionary<int, DbGameOdds>();

        var allOdds = await _dbContext.GameOdds
            .AsNoTracking()
            .Where(o => gameIds.Contains(o.GameId))
            .ToListAsync();

        return allOdds
            .GroupBy(o => o.GameId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.RunDateUTC).First());
    }

    private async Task AttachBookmakerOdds(List<GameOdds> gameOddsList)
    {
        var gameIds = gameOddsList.Select(g => g.Game.Id);
        var bookmakerOddsMap = await _bookmakerOddsRepo.GetBookmakerOddsByGameIds(gameIds);

        foreach (var gameOdds in gameOddsList)
        {
            if (bookmakerOddsMap.TryGetValue(gameOdds.Game.Id, out var bookmakerOdds))
                gameOdds.BookmakerOdds = bookmakerOdds;
        }
    }

    private async Task AttachSpreadTotalPredictions(List<GameOdds> gameOddsList)
    {
        var gameIds = gameOddsList.Select(g => g.Game.Id).ToHashSet();

        var latestPredictions = await _dbContext.GameSpreadTotalOdds
            .AsNoTracking()
            .Where(x => gameIds.Contains(x.GameId))
            .ToListAsync();

        var byGame = latestPredictions
            .GroupBy(x => new { x.GameId, x.ModelId })
            .Select(g => g.OrderByDescending(x => x.RunDateUTC).First())
            .GroupBy(x => x.GameId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var gameOdds in gameOddsList)
        {
            if (!byGame.TryGetValue(gameOdds.Game.Id, out var preds))
                continue;

            var spread = preds.FirstOrDefault(p => p.ModelId == 2);
            if (spread != null)
            {
                gameOdds.PredictedSpread = spread.PredictedValue;
                gameOdds.SpreadCoverProb = spread.CoverProbability;
            }

            var total = preds.FirstOrDefault(p => p.ModelId == 3);
            if (total != null)
            {
                gameOdds.PredictedTotal = total.PredictedValue;
                gameOdds.TotalOverProb = total.CoverProbability;
            }
        }
    }

    private async Task<Dictionary<int, DbSeasonTeam>> GetSeasonTeams(int seasonStartYear)
    {
        var teams = await _dbContext.SeasonTeam
            .AsNoTracking()
            .Where(x => x.SeasonStartYear == seasonStartYear)
            .ToListAsync();
        return teams.ToDictionary(t => t.TeamId);
    }

    public async Task<List<GameOdds>> GetAllGameOddsForSeason(int seasonStartYear)
    {
        var seasonTeams = await GetSeasonTeams(seasonStartYear);

        var games = await _dbContext.GameRaw
            .AsNoTracking()
            .Where(g => g.SeasonStartYear == seasonStartYear)
            .ToListAsync();

        var latestByGame = await GetLatestOddsForGames(games);
        var gameOdds = DbGameOddsToGameOddsMapper.Map(games, latestByGame, seasonTeams);

        await AttachBookmakerOdds(gameOdds);
        await AttachSpreadTotalPredictions(gameOdds);
        return gameOdds;
    }

    public async Task<DateTime?> GetAnchorDate(int seasonStartYear)
    {
        var utcNow = DateTime.UtcNow;
        var nowCentralDate = TimeZoneInfo.ConvertTimeFromUtc(utcNow, CentralZone).Date;
        var offset = CentralOffsetHours(utcNow);

        var upcoming = await _dbContext.GameRaw
            .AsNoTracking()
            .Where(g => g.SeasonStartYear == seasonStartYear
                && g.GameDateUTC.AddHours(offset).Date >= nowCentralDate)
            .OrderBy(g => g.GameDateUTC)
            .Select(g => (DateTime?)g.GameDateUTC)
            .FirstOrDefaultAsync();

        if (upcoming.HasValue)
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(upcoming.Value, DateTimeKind.Utc), CentralZone).Date;

        var past = await _dbContext.GameRaw
            .AsNoTracking()
            .Where(g => g.SeasonStartYear == seasonStartYear
                && g.GameDateUTC.AddHours(offset).Date < nowCentralDate)
            .OrderByDescending(g => g.GameDateUTC)
            .Select(g => (DateTime?)g.GameDateUTC)
            .FirstOrDefaultAsync();

        return past.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(past.Value, DateTimeKind.Utc), CentralZone).Date
            : null;
    }
}