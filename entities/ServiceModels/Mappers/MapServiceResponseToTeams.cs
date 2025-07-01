using Entities.Models;
using Entities.Models.Teams;

namespace Entities.ServiceModels.Mappers;

public static class MapServiceResponseToSeasonTeams
{
    /// <summary>
    /// Maps the standings response to a list of teams.
    /// Example response:
    /// https://api-web.nhle.com/v1/standings/2025-01-01
    /// </summary>
    /// <param name="standingsResponse">Nhl response that contains standings for teams</param>
    /// <returns>The list of teams</returns>
    public static IEnumerable<SeasonTeam>? Map(dynamic standingsResponse)
    {
        var teamList = new List<SeasonTeam>();
        foreach (var teamResponse in standingsResponse.standings)
        {
            var team = new SeasonTeam()
            {
                SeasonStartYear = (int)teamResponse.seasonStartYear // TODO: Make these real
                Name = teamResponse.name,
                Abbreviation = teamResponse.abbreviation,
                CommonName = teamResponse.commonName,
                LogoUri = teamResponse.logoUri,
                Division = teamResponse.division.name,
                DivisionAbbreviation = teamResponse.division.abbreviation,
                Conference = teamResponse.conference.name,
                ConferenceAbbreviation = teamResponse.conference.abbreviation,
                PlaceName = teamResponse.placeName
            };

            teamList.Add(team);
        }

        return teamList;
    }
}
