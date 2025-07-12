using Entities.DbModels;
using Entities.Models.Teams;

namespace Entities.Mappers.TeamMappers;

public static class MapDbSeasonTeamToTeam
{
    public static Team Map(DbSeasonTeam dbSeasonTeam)
    {
        if (dbSeasonTeam.Team == null)
        {
            throw new ArgumentException("DbSeasonTeam does not have a valid Team associated with it.");
        }

        var team = new Team()
        {
            Id = dbSeasonTeam.TeamId,
            Abbreviation = dbSeasonTeam.Team.Abbreviation,
            FranchiseId = dbSeasonTeam.Team.FranchiseId,
            LeagueId = dbSeasonTeam.Team.LeagueId,
            SeasonInformation = new Dictionary<int, SeasonTeam>()
            {
                { dbSeasonTeam.SeasonStartYear, MapDbSeasonTeamToSeasonTeam.Map(dbSeasonTeam) }
            }
        };

        return team;
    }
    public static IEnumerable<Team> MapList(IEnumerable<DbSeasonTeam> seasonTeams)
    {
        var teamList = new List<Team>();
        foreach (var seasonTeam in seasonTeams)
        {
            teamList.AddRange(Map(seasonTeam));
        }

        return teamList;
    }
}

