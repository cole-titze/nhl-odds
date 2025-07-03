using Entities.Models.Teams;

namespace Entities.ServiceModels.Mappers;

public static class MapStandingsResponseToSeasonTeams
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
                SeasonStartYear = (int)teamResponse.seasonId / 10000, // Assuming seasonId is in the format YYYYYYYY
                Name = teamResponse.teamName.@default,
                Abbreviation = teamResponse.teamAbbrev.@default,
                CommonName = teamResponse.teamCommonName.@default,
                LogoUri = teamResponse.teamLogo,
                Division = teamResponse.divisionName,
                DivisionAbbreviation = teamResponse.divisionAbbrev,
                Conference = (string?)teamResponse.conferenceName ?? "",
                ConferenceAbbreviation = (string?)teamResponse.conferenceAbbrev ?? "",
                PlaceName = teamResponse.placeName.@default,
            };

            teamList.Add(team);
        }

        return teamList;
    }
}
