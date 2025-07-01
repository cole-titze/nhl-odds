using Entities.DbModels;
using Entities.Models.Teams;

namespace Entities.Mappers.TeamMappers;

public static class MapSeasonTeamToDbSeasonTeam
{
    public static DbSeasonTeam Map(SeasonTeam team)
    {
        return new DbSeasonTeam()
        {
            TeamId = team.TeamId,
            SeasonStartYear = team.SeasonStartYear,
            Name = team.Name,
            Abbreviation = team.Abbreviation,
            CommonName = team.CommonName,
            LogoUri = team.LogoUri,
            Division = team.Division,
            DivisionAbbreviation = team.DivisionAbbreviation,
            Conference = team.Conference,
            ConferenceAbbreviation = team.ConferenceAbbreviation,
            PlaceName = team.PlaceName,
        };
    }
    public static IEnumerable<DbSeasonTeam> MapList(IEnumerable<SeasonTeam> teams)
    {
        var teamList = new List<DbSeasonTeam>();
        foreach (var team in teams)
        {
            teamList.Add(Map(team));
        }

        return teamList;
    }
}

