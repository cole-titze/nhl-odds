using Entities.DbModels;
using Entities.Models.Teams;

namespace Entities.Mappers.TeamMappers;

public static class MapTeamToDbTeam
{
    public static DbTeam Map(Team team)
    {
        return new DbTeam()
        {
            Id = team.Id,
            Abbreviation = team.Abbreviation,
        };
    }
    public static IEnumerable<DbTeam> MapList(IEnumerable<Team> teams)
    {
        var teamList = new List<DbTeam>();
        foreach (var team in teams)
        {
            teamList.Add(Map(team));
        }

        return teamList;
    }
}

