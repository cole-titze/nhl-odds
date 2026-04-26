using System.Text.Json.Nodes;
using Entities.Models.Teams;

namespace Entities.ServiceModels.Mappers;

public static class MapStandingsResponseToSeasonTeams
{
    public static IEnumerable<SeasonTeam>? Map(JsonNode? standingsResponse)
    {
        var teamList = new List<SeasonTeam>();
        foreach (var teamResponse in standingsResponse!["standings"]!.AsArray())
        {
            var team = new SeasonTeam()
            {
                SeasonStartYear = teamResponse!["seasonId"]!.GetValue<int>() / 10000,
                Name = teamResponse["teamName"]!["default"]!.GetValue<string>(),
                Abbreviation = teamResponse["teamAbbrev"]!["default"]!.GetValue<string>(),
                CommonName = teamResponse["teamCommonName"]!["default"]!.GetValue<string>(),
                LogoUri = teamResponse["teamLogo"]!.GetValue<string>(),
                Division = teamResponse["divisionName"]!.GetValue<string>(),
                DivisionAbbreviation = teamResponse["divisionAbbrev"]!.GetValue<string>(),
                Conference = teamResponse["conferenceName"]?.GetValue<string>() ?? "",
                ConferenceAbbreviation = teamResponse["conferenceAbbrev"]?.GetValue<string>() ?? "",
                PlaceName = teamResponse["placeName"]!["default"]!.GetValue<string>(),
            };

            teamList.Add(team);
        }

        return teamList;
    }
}