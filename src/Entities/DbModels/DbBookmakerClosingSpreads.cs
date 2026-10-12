using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

/// <summary>
/// Spread just before puck drop (one row per game and bookmaker), the closing counterpart of BookmakerSpreads.
/// Prices are American odds, as in BookmakerSpreads.
/// </summary>
[Table("BookmakerClosingSpreads")]
public class DbBookmakerClosingSpreads
{
    public int GameId { get; set; }
    [MaxLength(100)]
    public string BookmakerName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string BookmakerKey { get; set; } = string.Empty;
    public double HomePoint { get; set; }
    public int HomePrice { get; set; }
    public double AwayPoint { get; set; }
    public int AwayPrice { get; set; }
    /// <summary>When the book last moved the price (The Odds API market last_update; else the fetch or snapshot time)</summary>
    public DateTime PriceTimeUTC { get; set; }
    public DateTime FetchedDateUTC { get; set; }
    [MaxLength(100)]
    public string OddsApiGameId { get; set; } = string.Empty;
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
}
