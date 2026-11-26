using Entities.DbModels;
using Entities.Models.Web;

namespace DatabaseAccess.WebTeamRepository.Mappers;

public static class DbSeasonTeamToTeamStatsMapper
{
    public static TeamStats Map(DbSeasonTeam dbTeam)
    {
        return new TeamStats
        {
            Team = DbSeasonTeamToTeamMapper.Map(dbTeam)
        };
    }

    public static IEnumerable<TeamStats> MapList(IEnumerable<DbSeasonTeam> dbTeams)
    {
        return dbTeams.Select(Map).ToList();
    }
}
