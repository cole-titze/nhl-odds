using System.Text.Json.Nodes;
using Entities.Models.GamePlayEvents;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

public class ServiceGameEventResponse
{
    public readonly JsonNode? response;
    public ServiceGameEventResponse(JsonNode? incomingResponse)
    {
        response = incomingResponse;
    }
    public GameEvents GameEventsResponseToGameEvents()
    {
        return MapGameEventsResponseToGameEvents.Map(response);
    }
}
