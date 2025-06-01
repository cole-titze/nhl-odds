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
    public class ServiceResponse
    {
        public readonly dynamic? response;
        public ServiceResponse(dynamic incomingResponse)
        {
            response = incomingResponse;
        }
        public IEnumerable<IGamePlayerStats> GameStatsResponseToPlayerStats()
        {
            return MapGamePlayerStatsResponseToGamePlayerStats.Map(response);
        }
        public IEnumerable<IGamePlayerStats> CurentRosterResponseToPlayerStats()
        {
            return MapCurrentRosterResponseToGamePlayerStats.Map(response);
        }
        public Player PlayerResponseToPlayer()
        {
            return MapPlayerResponseToPlayer.Map(response);
        }
        public Game GameResponseToGame(ServiceResponse gameStatResponse)
        {
            return MapGameResponseToGame.Map(response, gameStatResponse);
        }
    }
}