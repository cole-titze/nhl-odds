using System.Text.Json.Nodes;
using Entities.Models;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

public class ServicePlayerResponse
{
    public readonly JsonNode? response;
    public ServicePlayerResponse(JsonNode? incomingResponse)
    {
        response = incomingResponse;
    }
    public Player PlayerResponseToPlayer(int gameTeamId)
    {
        return MapPlayerResponseToPlayer.Map(response, gameTeamId);
    }
}