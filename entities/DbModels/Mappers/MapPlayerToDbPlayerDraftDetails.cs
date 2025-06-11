using Entities.Models;

namespace Entities.DbModels.Mappers
{
    public static class MapPlayerToDbPlayerDraftDetails
    {
        public static DbPlayerDraftDetails? Map(Player player)
        {
            var playerDraftDetails = player.playerDraftDetails;
            if (playerDraftDetails == null)
                return null;

            return new DbPlayerDraftDetails()
            {
                playerId = player.id,
                year = playerDraftDetails.year,
                teamAbbrev = playerDraftDetails.teamAbbrev,
                round = playerDraftDetails.round,
                pickInRound = playerDraftDetails.pickInRound,
                overallPick = playerDraftDetails.overallPick,
            };
        }
        public static IEnumerable<DbPlayerDraftDetails> MapList(IEnumerable<Player> players)
        {
            var playersDraftDetails = new List<DbPlayerDraftDetails>();
            foreach (var player in players)
            {
                var playerDraftDetails = Map(player);
                if (playerDraftDetails != null)
                    playersDraftDetails.Add(playerDraftDetails);
            }

            return playersDraftDetails;
        }
    }
}