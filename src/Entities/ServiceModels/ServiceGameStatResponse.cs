using System.Text.Json.Nodes;
using Entities.Models;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

public class ServiceGameStatResponse
{
    public readonly JsonNode? response;
    public ServiceGameStatResponse(JsonNode? incomingResponse)
    {
        response = incomingResponse;
    }
    public GameRosterStats GameStatsResponseToGameRosterStats(ServiceGameSummaryResponse gameSummaryResponse)
    {
        return MapGamePlayerStatsResponseToGamePlayerStats.Map(gameSummaryResponse.response, response);
    }
    public Game GameStatResponseToGame(ServiceGameSummaryResponse gameSummaryResponse, ServiceGameEventResponse gameEventResponse)
    {
        return MapGameResponseToGame.Map(gameSummaryResponse.response, response, gameEventResponse.response);
    }
}