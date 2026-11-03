using Entities.Types;

namespace Entities.Models.Web;

/// <summary>
/// One played game's inputs for the Track Record page: the model's home win probability, whether it
/// was saved before puck drop, and every bookmaker's morning and closing price.
/// </summary>
public class TrackRecordGame
{
    public int GameId { get; set; }
    public DateTime GameDateUTC { get; set; }
    public Winner Winner { get; set; }
    public double ModelHomeOdds { get; set; }
    /// <summary>
    /// True when the prediction was saved before puck drop. False for walk-forward backfill rows, which
    /// are saved after the game (trained only on earlier games, but run later).
    /// </summary>
    public bool IsLive { get; set; }
    /// <summary>Morning prices (BookmakerOdds), vig-included implied probabilities</summary>
    public List<TrackRecordPrice> Morning { get; set; } = [];
    /// <summary>Closing prices (BookmakerClosingOdds), vig-included implied probabilities</summary>
    public List<TrackRecordPrice> Closing { get; set; } = [];
}

public record TrackRecordPrice(string BookmakerName, double HomeOdds, double AwayOdds);
