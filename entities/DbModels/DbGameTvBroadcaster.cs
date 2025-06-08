using System.ComponentModel.DataAnnotations;

namespace Entities.DbModels
{
    public class DbGameTvBroadcaster
    {
        [Key]
        public int broadcasterId { get; set; }
        public int gameId { get; set; }
        public void Clone(DbGameTvBroadcaster gameTvBroadcaster)
        {
            broadcasterId = gameTvBroadcaster.broadcasterId;
            gameId = gameTvBroadcaster.gameId;
        }
    }
}