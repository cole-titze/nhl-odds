using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

[Table("BookmakerOdds")]
public class DbBookmakerOdds
{
    public int GameId { get; set; }
    [MaxLength(100)]
    public string BookmakerName { get; set; } = string.Empty;
    public double HomeOdds { get; set; }
    public double AwayOdds { get; set; }
    public DateTime FetchedDateUTC { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
}
