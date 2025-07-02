using Entities.DbModels;
using Entities.Mappers.TeamMappers;
using Entities.Models.Teams;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.TeamRepository;

public class TeamRepository : ITeamRepository
{
    private readonly NhlDbContext _dbContext;
    public TeamRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets a single database team
    /// </summary>
    /// <param name="teamId">The team to get</param>
    /// <param name="seasonStartYear">The season start year</param>
    /// <returns>The team for the given season</returns>
    private async Task<DbSeasonTeam?> GetDbSeasonTeam(int teamId, int seasonStartYear)
    {
        return await _dbContext.SeasonTeam.Where(x => x.TeamId == teamId && x.SeasonStartYear == seasonStartYear).FirstOrDefaultAsync();
    }
    /// <summary>
    /// Gets a single team for a given season
    /// </summary>
    /// <param name="teamId">The team to get</param>
    /// <param name="seasonStartYear">The season start year</param>
    /// <returns>The team for the given season</returns>
    private async Task<DbTeam?> GetDbTeam(int teamId)
    {
        return await _dbContext.Team.Where(x => x.Id == teamId).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Adds teams if they do not exist, or updates them if they do.
    /// </summary>
    /// <param name="teams">The teams to add</param>
    public async Task AddUpdateSeasonTeams(IEnumerable<Team> teams)
    {
        var dbSeasonTeams = MapTeamToDbSeasonTeam.MapList(teams);

        var addList = new List<DbSeasonTeam>();
        var updateList = new List<DbSeasonTeam>();
        foreach (var seasonTeam in dbSeasonTeams)
        {
            var dbSeasonTeam = await GetDbSeasonTeam(seasonTeam.TeamId, seasonTeam.SeasonStartYear);
            if (dbSeasonTeam == null)
            {
                addList.Add(seasonTeam);
            }
            else if (!dbSeasonTeam.IsEquivalentTo(seasonTeam))
            {
                dbSeasonTeam.Clone(seasonTeam);
                updateList.Add(dbSeasonTeam);
            }
        }

        await _dbContext.SeasonTeam.AddRangeAsync(addList);
        _dbContext.SeasonTeam.UpdateRange(updateList);
    }

    /// <summary>
    /// Adds teams if they do not exist, or updates them if they do.
    /// </summary>
    /// <param name="teams">The teams to add</param>
    public async Task AddUpdateTeams(IEnumerable<Team> teams)
    {
        var dbTeams = MapTeamToDbTeam.MapList(teams);

        var addList = new List<DbTeam>();
        var updateList = new List<DbTeam>();
        foreach (var team in dbTeams)
        {
            var dbTeam = await GetDbTeam(team.Id);
            if (dbTeam == null)
            {
                addList.Add(team);
            }
            else if (!dbTeam.IsEquivalentTo(team))
            {
                dbTeam.Clone(team);
                updateList.Add(dbTeam);
            }
        }

        await _dbContext.Team.AddRangeAsync(addList);
        _dbContext.Team.UpdateRange(updateList);
    }

    /// <summary>
    /// Checks if a season has teams already found in the database.
    /// </summary>
    /// <param name="seasonStartYear">The year to check</param>
    /// <returns>True if the teams have been found, otherwise false</returns>
    public async Task<bool> HasSeasonTeams(int seasonStartYear)
    {
        return await _dbContext.SeasonTeam.AnyAsync(x => x.SeasonStartYear == seasonStartYear);
    }

    /// <summary>
    /// Commits the changes to the database.
    /// </summary>
    public async Task Commit()
    {
        await _dbContext.SaveChangesAsync();
    }
}
