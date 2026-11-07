using Entities.Models.Web;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebPredictionFactorRepository;

public class WebPredictionFactorRepository : IWebPredictionFactorRepository
{
    private readonly GameDbContext _dbContext;

    public WebPredictionFactorRepository(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PredictionFactors?> GetForLatestPrediction(int gameId)
    {
        // Same choice as WebGameOddsRepository: the newest run, so the factors match the odds shown
        var latest = await _dbContext.GameOdds
            .AsNoTracking()
            .Where(o => o.GameId == gameId)
            .OrderByDescending(o => o.RunDateUTC)
            .Select(o => new { o.ModelId, o.RunDateUTC })
            .FirstOrDefaultAsync();
        if (latest is null)
            return null;

        var rows = await _dbContext.GamePredictionFactor
            .AsNoTracking()
            .Where(f => f.GameId == gameId && f.ModelId == latest.ModelId && f.RunDateUTC == latest.RunDateUTC)
            .OrderBy(f => f.Rank)
            .ToListAsync();
        if (rows.Count == 0)
            return null;

        return new PredictionFactors
        {
            GameId = gameId,
            RunDateUTC = DateTime.SpecifyKind(latest.RunDateUTC, DateTimeKind.Utc),
            BaseHomeOdds = rows[0].BaseHomeOdds,
            Factors = rows.Select(f => new PredictionFactor
            {
                Factor = f.Factor,
                Detail = f.Detail,
                Contribution = f.Contribution,
            }).ToList(),
        };
    }
}
