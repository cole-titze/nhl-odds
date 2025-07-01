using Entities.DbModels;
using Entities.Models.Teams;

namespace Entities.Mappers.TeamMappers;

public static class MapDbTeamToTeam
{
    public static Team Map(DbTeam team)
    {
        return new Team()
        {
            Id = team.Id,
            Abbreviation = team.Abbreviation,
        };
    }
}

