using System.Net;
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
    public class ServiceGameSummaryResponse
    {
        public readonly dynamic? response;
        public ServiceGameSummaryResponse(dynamic incomingResponse)
        {
            response = incomingResponse;
        }
        /// <summary>
        /// Converts the game summary response to a game object.
        /// </summary>
        /// <param name="gameStatResponse">The game stats response</param>
        /// <returns>The game object</returns>
        public Game GameSummaryResponseToGame(ServiceGameStatResponse gameStatResponse)
        {
            return MapGameResponseToGame.Map(response, gameStatResponse);
        }
        /// <summary>
        /// Determines if a game is done or not
        /// </summary>
        /// <param name="message">response from nhl api</param>
        /// <returns>True if game is done, otherwise false</returns>
        public bool IsGameDone()
        {
            if (response == null)
                return false;

            if (response.gameState == "OFF")
                return true;

            return false;
        }
        /// <summary>
        /// Determines if a game is yet to be played
        /// </summary>
        /// <param name="message">response from nhl api</param>
        /// <returns>True if game is in the future, otherwise false</returns>
        public bool IsGameFuture()
        {
            if (response == null)
                return false;

            if (response.gameState == "FUT")
                return true;

            return false;
        }
        /// <summary>
        /// Determines if a game is in progress or not
        /// </summary>
        /// <param name="message">response from nhl api</param>
        /// <returns>True if game is in progress, otherwise false</returns>
        public bool IsGameInProgress()
        {
            if (response == null)
                return false;

            if (response.gameState != "OFF" && response.gameState != "FUT")
                return true;

            return false;
        }
    }
}