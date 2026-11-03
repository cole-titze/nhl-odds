namespace Entities.ViewModels;

/// <summary>How the market's view of a game moved before puck drop, next to the model's</summary>
public class OddsMovementVM
{
    public int GameId { get; set; }
    public DateTime StartUTC { get; set; }
    /// <summary>Model first, then the partner widgets, the bookmakers' average and Kalshi; empty series are left out</summary>
    public List<OddsSeriesVM> Series { get; set; } = [];
    /// <summary>The bookmakers' average from morning to close, when both are known</summary>
    public MarketMoveVM? Move { get; set; }
}

public class OddsSeriesVM
{
    public string Name { get; set; } = string.Empty;
    /// <summary>"model", "partner", "average" or "kalshi"</summary>
    public string Kind { get; set; } = string.Empty;
    public List<OddsPointVM> Points { get; set; } = [];
}

public class OddsPointVM
{
    public DateTime TimeUTC { get; set; }
    /// <summary>Home win probability, bookmaker margin removed</summary>
    public double HomeProbability { get; set; }
}

public class MarketMoveVM
{
    public double Open { get; set; }
    public double Close { get; set; }
    /// <summary>The model's last prediction before puck drop, if any</summary>
    public double? Model { get; set; }
    /// <summary>Close minus open, signed toward the model (positive: the market moved toward it)</summary>
    public double? TowardModel { get; set; }
}
