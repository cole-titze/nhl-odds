using System.Text.Json.Nodes;

namespace Entities.ServiceModels.Mappers;

public static class MapScheduleResponseToGameCount
{
    public static int Map(JsonNode? scheduleResponse, int seasonId)
    {
        int seasonGameCount = 0;
        foreach (var season in scheduleResponse!["data"]!.AsArray())
        {
            if (season!["id"]!.GetValue<int>() == seasonId)
                seasonGameCount = season["totalRegularSeasonGames"]!.GetValue<int>();
        }
        return seasonGameCount;
    }
}
