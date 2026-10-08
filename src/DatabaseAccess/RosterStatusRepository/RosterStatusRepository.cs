using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.RosterStatusRepository;

public class RosterStatusRepository : IRosterStatusRepository
{
    private readonly NhlDbContext _dbContext;

    public RosterStatusRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<RosterReportCandidate>> GetCandidates(IEnumerable<DateTime> birthDates, int sinceSeasonStartYear)
    {
        var dates = birthDates.Select(d => d.Date).Distinct().ToList();
        var players = await _dbContext.Player
            .Where(p => dates.Contains(p.BirthDate))
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.BirthDate })
            .ToListAsync();
        var ids = players.Select(p => p.Id).ToList();

        var skaterGames = await _dbContext.GameSkaterStats
            .Where(s => ids.Contains(s.PlayerId) && s.Game!.SeasonStartYear >= sinceSeasonStartYear)
            .Select(s => new { s.PlayerId, s.TeamId, s.Game!.GameDateUTC })
            .ToListAsync();
        var goalieGames = await _dbContext.GameGoalieStats
            .Where(s => ids.Contains(s.PlayerId) && s.Game!.SeasonStartYear >= sinceSeasonStartYear)
            .Select(s => new { s.PlayerId, s.TeamId, s.Game!.GameDateUTC })
            .ToListAsync();
        var latestTeam = skaterGames.Concat(goalieGames)
            .GroupBy(s => s.PlayerId)
            .ToDictionary(g => g.Key, g => g.MaxBy(s => s.GameDateUTC)!.TeamId);

        return players
            .Where(p => latestTeam.ContainsKey(p.Id))
            .Select(p => new RosterReportCandidate(p.Id, p.FirstName, p.LastName, p.BirthDate, latestTeam[p.Id]))
            .ToList();
    }

    public async Task AddSnapshot(IEnumerable<DbRosterStatus> statuses)
    {
        _dbContext.RosterStatus.AddRange(statuses);
        await _dbContext.SaveChangesAsync();
    }
}