using Entities.Models.Teams;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels;

/// <summary>
/// Creating objects for external service API responses can be challenging 
/// due to not having control over changes to the responses. I've been following 
/// the dynamic pattern so I don't have to create models that contain all 
/// information from the API. This way all service models are just dynamic objects 
/// and the mappers handle transforming them into the business logic models.
/// </summary>
public class ServiceStandingsResponse
{
    public readonly dynamic? response;
    public ServiceStandingsResponse(dynamic incomingResponse)
    {
        response = incomingResponse;
    }
    /// <summary>
    /// Converts the standings response to a list of teams.
    /// </summary>
    /// <returns>Teams</returns>
    public IEnumerable<SeasonTeam>? StandingsResponseToSeasonTeams()
    {
        return MapServiceResponseToSeasonTeams.Map(response);
    }
}