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

        return IsFinishedState(response["gameState"]!.GetValue<string>());
    }
    /// <summary>
    /// FINAL is a finished game the NHL hasn't made official yet (it turns OFF hours later). The stats are complete,
    /// and the nightly re-fetch picks up any corrections made before it goes OFF.
    /// </summary>
    public static bool IsFinishedState(string gameState) => gameState is "OFF" or "FINAL";
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
        if (!IsFinishedState(state) && state != "FUT")
            return true;

        return false;
    }
}