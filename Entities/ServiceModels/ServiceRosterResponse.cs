using System.Text.Json.Nodes;
using Entities.Models;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

public class ServiceRosterResponse
{
    private readonly JsonNode? homeResponse;
    private readonly JsonNode? awayResponse;
    public ServiceRosterResponse(JsonNode? homeTeamResponse, JsonNode? awayTeamResponse)
    {
        homeResponse = homeTeamResponse;
        awayResponse = awayTeamResponse;
    }
    public GameRosterStats CurentRosterResponseToGameRosterStats(int homeTeamId, int awayTeamId)
    {
        return MapCurrentRosterResponseToGamePlayerStats.Map(homeResponse, awayResponse, homeTeamId, awayTeamId);
    }
}
