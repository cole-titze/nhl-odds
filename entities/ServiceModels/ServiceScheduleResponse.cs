using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

/// <summary>
/// Creating objects for external service API responses can be challenging 
/// due to not having control over changes to the responses. I've been following 
/// the dynamic pattern so I don't have to create models that contain all 
/// information from the API. This way all service models are just dynamic objects 
/// and the mappers handle transforming them into the business logic models.
/// </summary>
public class ServiceScheduleResponse
{
    public readonly dynamic? response;
    public ServiceScheduleResponse(dynamic incomingResponse)
    {
        response = incomingResponse;
    }
    /// <summary>
    /// Converts the schedule response to a count of games for the given season id.
    /// </summary>
    /// <param name="seasonId">The season id (Ex. 20232024)</param>
    /// <returns>Game count for the season</returns>
    public int ScheduleResponseToGameCount(int seasonId)
    {
        return MapScheduleResponseToGameCount.Map(response, seasonId);
    }
}