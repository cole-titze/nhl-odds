using System.Text.Json.Nodes;
using Entities.Models.Teams;

namespace Entities.ServiceModels.Mappers;

public static class MapAllTeamsResponseToTeams
{
    public static IEnumerable<Team> Map(JsonNode? standingsResponse)
    {
        var teamList = new List<Team>();
        foreach (var teamResponse in standingsResponse!["data"]!.AsArray())
        {
            var team = new Team()
            {
                Id = teamResponse!["id"]!.GetValue<int>(),
                FranchiseId = teamResponse["franchiseId"]?.GetValue<int>() ?? -1,
                LeagueId = teamResponse["leagueId"]!.GetValue<int>(),
                Abbreviation = teamResponse["triCode"]!.GetValue<string>(),
            };

            teamList.Add(team);
        }

        return teamList;
    }
}
