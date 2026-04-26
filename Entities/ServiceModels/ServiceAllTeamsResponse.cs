using System.Text.Json.Nodes;
using Entities.Models.Teams;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

public class ServiceAllTeamsResponse
{
    public readonly JsonNode? response;
    public ServiceAllTeamsResponse(JsonNode? incomingResponse)
    {
        response = incomingResponse;
    }
    public IEnumerable<Team> AllTeamsResponseToTeams()
    {
        return MapAllTeamsResponseToTeams.Map(response);
    }
}