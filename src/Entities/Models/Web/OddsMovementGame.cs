namespace Entities.Models.Web;

/// <summary>Every price and prediction saved for one game before puck drop (the Game page's odds movement chart)</summary>
public class OddsMovementGame
{
    public int GameId { get; set; }
    public DateTime StartUTC { get; set; }
    public List<ModelRun> ModelRuns { get; set; } = [];
    /// <summary>Morning prices (BookmakerOdds), vig-included implied probabilities</summary>
    public List<BookPrice> Morning { get; set; } = [];
    /// <summary>Closing prices (BookmakerClosingOdds), vig-included implied probabilities</summary>
    public List<BookPrice> Closing { get; set; } = [];
    /// <summary>Two-way moneyline versions from NHL.com's betting-partner widget, in American odds</summary>
    public List<PartnerPrice> Partner { get; set; } = [];
}

public record ModelRun(DateTime TimeUTC, double HomeOdds);

public record BookPrice(string BookmakerName, DateTime TimeUTC, double HomeOdds, double AwayOdds);

/// <param name="Source">The partner and country, e.g. "DraftKings (US)"</param>
public record PartnerPrice(string Source, DateTime TimeUTC, int HomePrice, int AwayPrice);
