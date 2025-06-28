using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

public class DbGameTvBroadcaster
{
    [Key]
    public int BroadcasterId { get; set; }
    public int GameId { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    [ForeignKey(nameof(BroadcasterId))]
    public DbTvBroadcaster? Broadcaster { get; set; }
    public void Clone(DbGameTvBroadcaster gameTvBroadcaster)
    {
        BroadcasterId = gameTvBroadcaster.BroadcasterId;
        GameId = gameTvBroadcaster.GameId;
    }
}