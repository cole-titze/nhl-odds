using System.Text.Json.Nodes;
using Entities.Models.Teams;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

public class ServiceStandingsResponse
{
    public readonly JsonNode? response;
    public ServiceStandingsResponse(JsonNode? incomingResponse)
    {
        response = incomingResponse;
    }
    public IEnumerable<SeasonTeam>? StandingsResponseToSeasonTeams()
    {
        return MapStandingsResponseToSeasonTeams.Map(response);
    }
}