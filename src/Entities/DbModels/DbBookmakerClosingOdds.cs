using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

/// <summary>
/// Moneyline price just before puck drop (one row per game and bookmaker), kept apart from BookmakerOdds, which
/// holds the morning price the predictor sees. Odds are vig-included implied probabilities, as in BookmakerOdds.
/// </summary>
[Table("BookmakerClosingOdds")]
public class DbBookmakerClosingOdds
{
    public int GameId { get; set; }
    [MaxLength(100)]
    public string BookmakerName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string BookmakerKey { get; set; } = string.Empty;
    public double HomeOdds { get; set; }
    public double AwayOdds { get; set; }
    /// <summary>When the price was quoted (Kalshi: end of the last one-minute candle before the start)</summary>
    public DateTime PriceTimeUTC { get; set; }
    public DateTime FetchedDateUTC { get; set; }
    [MaxLength(100)]
    public string OddsApiGameId { get; set; } = string.Empty;
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
}