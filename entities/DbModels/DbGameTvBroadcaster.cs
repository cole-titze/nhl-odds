using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels
{
    public class DbGameTvBroadcaster
    {
        [Key]
        public int broadcasterId { get; set; }
        public int gameId { get; set; }
        [ForeignKey("gameId")]
        public DbGameRaw? game { get; set; }
        [ForeignKey("broadcasterId")]
        public DbTvBroadcaster? broadcaster { get; set; }
        public void Clone(DbGameTvBroadcaster gameTvBroadcaster)
        {
            broadcasterId = gameTvBroadcaster.broadcasterId;
            gameId = gameTvBroadcaster.gameId;
        }
    }
}