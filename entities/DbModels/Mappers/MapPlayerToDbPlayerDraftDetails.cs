using Entities.Models;

namespace Entities.DbModels.Mappers;

public static class MapPlayerToDbPlayerDraftDetails
{
    public static DbPlayerDraftDetails? Map(Player player)
    {
        var playerDraftDetails = player.PlayerDraftDetails;
        if (playerDraftDetails == null)
            return null;

        return new DbPlayerDraftDetails()
        {
            PlayerId = player.Id,
            Year = playerDraftDetails.Year,
            TeamAbbrev = playerDraftDetails.TeamAbbrev,
            Round = playerDraftDetails.Round,
            PickInRound = playerDraftDetails.PickInRound,
            OverallPick = playerDraftDetails.OverallPick,
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