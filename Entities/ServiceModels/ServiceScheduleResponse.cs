using System.Text.Json.Nodes;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

public class ServiceScheduleResponse
{
    public readonly JsonNode? response;
    public ServiceScheduleResponse(JsonNode? incomingResponse)
    {
        response = incomingResponse;
    }
    public int ScheduleResponseToGameCount(int seasonId)
    {
        return MapScheduleResponseToGameCount.Map(response, seasonId);
    }
}
