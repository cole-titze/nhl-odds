using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.LineupArticleRepository;

public class LineupArticleRepository : ILineupArticleRepository
{
    private readonly NhlDbContext _dbContext;

    public LineupArticleRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> Exists(string contentHash)
    {
        return _dbContext.LineupArticle.AnyAsync(a => a.ContentHash == contentHash);
    }

    public async Task Add(DbLineupArticle article)
    {
        _dbContext.LineupArticle.Add(article);
        await _dbContext.SaveChangesAsync();
    }

    public Task<List<DbLineupArticle>> GetUnparsedVersions()
    {
        return _dbContext.LineupArticle.AsNoTracking()
            .Where(a => !_dbContext.LineupArticleParse.Any(p => p.ArticleHash == a.ContentHash))
            .OrderBy(a => a.FirstSeenUTC)
            .ToListAsync();
    }

    public Task<bool> SectionExists(string sectionHash)
    {
        return _dbContext.LineupGame.AnyAsync(g => g.SectionHash == sectionHash);
    }

    public async Task<List<DbSeasonTeam>> GetLatestSeasonTeams()
    {
        var season = await _dbContext.SeasonTeam.MaxAsync(t => t.SeasonStartYear);
        return await _dbContext.SeasonTeam.AsNoTracking().Where(t => t.SeasonStartYear == season).ToListAsync();
    }

    public Task<int?> FindGameId(int awayTeamId, int homeTeamId, DateTime fromUtc, DateTime toUtc)
    {
        return _dbContext.GameRaw.AsNoTracking()
            .Where(g => g.AwayTeamId == awayTeamId && g.HomeTeamId == homeTeamId && g.GameDateUTC >= fromUtc && g.GameDateUTC <= toUtc)
            .OrderBy(g => g.GameDateUTC)
            .Select(g => (int?)g.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<(int Id, string FirstName, string LastName)>> GetPlayerNames()
    {
        var players = await _dbContext.Player.AsNoTracking()
            .Select(p => new { p.Id, p.FirstName, p.LastName })
            .ToListAsync();
        return players.Select(p => (p.Id, p.FirstName, p.LastName)).ToList();
    }

    public async Task AddParsed(IEnumerable<DbLineupGame> games, IEnumerable<DbLineupPlayer> players, DbLineupArticleParse parse)
    {
        _dbContext.LineupGame.AddRange(games);
        _dbContext.LineupPlayer.AddRange(players);
        _dbContext.LineupArticleParse.Add(parse);
        await Save();
    }

    public async Task AddParseFailure(DbLineupArticleParse parse)
    {
        _dbContext.LineupArticleParse.Add(parse);
        await Save();
    }

    // Clears tracking even when the save fails, so one bad version doesn't break the next one
    private async Task Save()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }
}