using System.Text.Json.Nodes;
using Entities.Models;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

public class ServiceGameSummaryResponse
{
    public readonly JsonNode? response;
    public ServiceGameSummaryResponse(JsonNode? incomingResponse)
    {
        response = incomingResponse;
    }
    public Game GameSummaryResponseToGame(ServiceGameStatResponse gameStatResponse, ServiceGameEventResponse gameEventResponse)
    {
        return MapGameResponseToGame.Map(response, gameStatResponse.response, gameEventResponse.response);
    }
    public bool IsGameDone()
    {
        if (response == null)
            return false;

        if (response["gameState"]!.GetValue<string>() == "OFF")
            return true;

        return false;
    }
    public bool IsGameFuture()
    {
        if (response == null)
            return false;

        if (response["gameState"]!.GetValue<string>() == "FUT")
            return true;

        return false;
    }
    public bool IsGameInProgress()
    {
        if (response == null)
            return false;

        var state = response["gameState"]!.GetValue<string>();
        if (state != "OFF" && state != "FUT")
            return true;

        return false;
    }
}