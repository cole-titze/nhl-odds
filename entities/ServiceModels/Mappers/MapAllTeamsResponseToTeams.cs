using Entities.Models;
using Entities.Models.Teams;

namespace Entities.ServiceModels.Mappers;

public static class MapAllTeamsResponseToTeams
{
    /// <summary>
    /// Maps the standings response to a list of teams.
    /// Example response:
    /// https://api-web.nhle.com/v1/standings/2025-01-01
    /// </summary>
    /// <param name="standingsResponse">Nhl response that contains standings for teams</param>
    /// <returns>The list of teams</returns>
    public static IEnumerable<Team>? Map(dynamic standingsResponse)
    {
        var teamList = new List<Team>();
        foreach (var teamResponse in standingsResponse.data)
        {
            var team = new Team()
            {
                Id = teamResponse.id,
                FranchiseId = teamResponse.franchiseId,
                LeagueId = teamResponse.leagueId,
                Abbreviation = teamResponse.triCode,
            };

            teamList.Add(team);
        }

        return teamList;
    }
}
