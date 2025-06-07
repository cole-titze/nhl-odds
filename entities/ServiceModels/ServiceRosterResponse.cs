using Entities.Models;
using Entities.ServiceModels.Mappers;

namespace Entities.ServiceModels
{
    /// <summary>
    /// Creating objects for external service API responses can be challenging 
    /// due to not having control over changes to the responses. I've been following 
    /// the dynamic pattern so I don't have to create models that contain all 
    /// information from the API. This way all service models are just dynamic objects 
    /// and the mappers handle transforming them into the business logic models.
    /// </summary>
    public class ServiceRosterResponse
    {
        public readonly dynamic? response;
        public ServiceRosterResponse(dynamic incomingResponse)
        {
            response = incomingResponse;
        }
        /// <summary>
        /// Converts the current roster response to a collection of game player stats.
        /// </summary>
        /// <returns>The mapped game player stats</returns>
        public IEnumerable<IGamePlayerStats> CurentRosterResponseToPlayerStats(int gameId, int teamId)
        {
            return MapCurrentRosterResponseToGamePlayerStats.Map(response, gameId, teamId);
        }
    }
}