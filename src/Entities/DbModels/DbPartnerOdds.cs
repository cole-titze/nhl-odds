namespace Entities.DbModels;

/// <summary>
/// One team's odds line from a version of NHL.com's betting-partner odds widget (DraftKings in the US, FanDuel in
/// Canada). The widget only shows the current odds, so each version is saved the first time it is seen. After puck
/// drop it shows live odds, so pre-game lines are the rows with PartnerUpdatedUTC before StartTimeUTC.
/// </summary>
public class DbPartnerOdds
{
    // "US" or "CA"
    public string Country { get; set; } = string.Empty;
    // The widget's lastUpdatedUTC: identifies the version
    public DateTime PartnerUpdatedUTC { get; set; }
    public int GameId { get; set; }
    public int TeamId { get; set; }
    // MONEY_LINE_2_WAY, MONEY_LINE_2_WAY_TNB, MONEY_LINE_3_WAY, PUCK_LINE or OVER_UNDER
    public string Market { get; set; } = string.Empty;
    // The line or outcome as the widget shows it, e.g. "+1.5", "O6.5", "Draw"; empty for plain moneylines
    public string Qualifier { get; set; } = string.Empty;
    // The qualifier's number: the puck line (+1.5) or total (6.5); null for moneylines
    public decimal? Line { get; set; }
    // "Over" or "Under" for totals, "Draw" for the 3-way draw; null otherwise
    public string? Outcome { get; set; }
    // American odds
    public decimal Price { get; set; }
    public bool IsHome { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public DateTime? OddsDate { get; set; }
    public DateTime StartTimeUTC { get; set; }
    public DateTime FirstSeenUTC { get; set; }
}