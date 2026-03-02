using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.GameSeasonRepository;

public class GameSeasonRepository : IGameSeasonRepository
{
    private readonly NhlDbContext _dbContext;

    public GameSeasonRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Game>> GetSeasonGames(int seasonStartYear)
    {
        var dbGames = await _dbContext.GameRaw
            .Where(x => x.SeasonStartYear == seasonStartYear)
            .ToListAsync();

        return dbGames.Select(g => new Game
        {
            Id = g.Id,
            HomeTeamId = g.HomeTeamId,
            AwayTeamId = g.AwayTeamId,
            SeasonStartYear = g.SeasonStartYear,
            GameDateUTC = g.GameDateUTC,
            HomeGoals = g.HomeGoals,
            AwayGoals = g.AwayGoals,
            Winner = g.Winner,
            EndPeriod = g.EndPeriod,
            HomeSOG = g.HomeSOG,
            AwaySOG = g.AwaySOG,
            HomePPG = g.HomePPG,
            AwayPPG = g.AwayPPG,
            HomePIM = g.HomePIM,
            AwayPIM = g.AwayPIM,
            HomeFaceOffWinPercent = g.HomeFaceOffWinPercent,
            AwayFaceOffWinPercent = g.AwayFaceOffWinPercent,
            HomeBlockedShots = g.HomeBlockedShots,
            AwayBlockedShots = g.AwayBlockedShots,
            HomeHits = g.HomeHits,
            AwayHits = g.AwayHits,
            HomeTakeaways = g.HomeTakeaways,
            AwayTakeaways = g.AwayTakeaways,
            HomeGiveaways = g.HomeGiveaways,
            AwayGiveaways = g.AwayGiveaways,
            HasBeenPlayed = g.HasBeenPlayed,
        }).ToList();
    }
}
