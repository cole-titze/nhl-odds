using Entities.DbModels;
using Entities.Models.Teams;

namespace Entities.Mappers.TeamMappers;

public static class MapTeamToDbSeasonTeam
{
    public static IEnumerable<DbSeasonTeam> Map(Team team)
    {
        var seasonTeams = new List<DbSeasonTeam>();

        foreach (var season in team.SeasonInformation)
        {
            var seasonStartYear = season.Key;
            var teamSeason = season.Value;
            var dbSeasonTeam = new DbSeasonTeam()
            {
                TeamId = team.Id,
                SeasonStartYear = seasonStartYear,
                Name = teamSeason.Name,
                Abbreviation = teamSeason.Abbreviation,
                CommonName = teamSeason.CommonName,
                LogoUri = teamSeason.LogoUri,
                Division = teamSeason.Division,
                DivisionAbbreviation = teamSeason.DivisionAbbreviation,
                Conference = teamSeason.Conference,
                ConferenceAbbreviation = teamSeason.ConferenceAbbreviation,
                PlaceName = teamSeason.PlaceName,
            };
            seasonTeams.Add(dbSeasonTeam);
        }

        return seasonTeams;
    }
    public static IEnumerable<DbSeasonTeam> MapList(IEnumerable<Team> teams)
    {
        var teamList = new List<DbSeasonTeam>();
        foreach (var team in teams)
        {
            teamList.AddRange(Map(team));
        }

        return teamList;
    }
}

