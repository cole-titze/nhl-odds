using System.Numerics;
using Entities.DbModels;
using Entities.Models;
using Entities.Types.Mappers;

namespace Entities.ServiceModels.Mappers
{
    public static class MapGamePlayerStatsResponseToGamePlayerStats
    {
        /// <summary>
        /// Builds a player stats object
        /// </summary>
        /// <param name="playerStatResponse">Player stat response</param>
        /// <returns>Player stats object</returns>
		public static IGamePlayerStats Map(dynamic gamePlayerStatResponse)
        {
            return new GameGoalieStats();
        }
    }
}

