using Entities.Models.GamePlayEvents;
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
    public class ServiceGameEventResponse
    {
        public readonly dynamic? response;
        public ServiceGameEventResponse(dynamic incomingResponse)
        {
            response = incomingResponse;
        }
        /// <summary>
        /// Converts the game stats response to a collection of game player stats.
        /// </summary>
        /// <param name="gameSummaryResponse"></param>
        /// <returns>The list of player stats for the game</returns>
        public GameEvents GameEventsResponseToGameEvents()
        {
            return MapGameEventsResponseToGameEvents.Map(response);
        }
    }
}