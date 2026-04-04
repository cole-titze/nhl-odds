using DatabaseAccess.WebTeamRepository.Mappers;
using Entities.Models.Web;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebTeamRepository;

public class TeamRepository : ITeamRepository
{
    private readonly GameDbContext _dbContext;
    public TeamRepository(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<TeamStats>> GetAllTeams(int seasonStartYear)
    {
        var dbTeams = await _dbContext.SeasonTeam
            .AsNoTracking()
            .Where(x => x.SeasonStartYear == seasonStartYear)
            .ToListAsync();
        return DbSeasonTeamToTeamStatsMapper.MapList(dbTeams);
    }

    public async Task<TeamStats> GetTeam(int teamId, int seasonStartYear)
    {
        var dbTeam = await _dbContext.SeasonTeam
            .AsNoTracking()
            .Where(x => x.TeamId == teamId && x.SeasonStartYear == seasonStartYear)
            .FirstAsync();
        return DbSeasonTeamToTeamStatsMapper.Map(dbTeam);
    }
}
