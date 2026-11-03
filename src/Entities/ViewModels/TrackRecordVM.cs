namespace Entities.ViewModels;

/// <summary>How the model's win probabilities held up over one season (the Track Record page)</summary>
public class TrackRecordVM
{
    public int SeasonStartYear { get; set; }
    public bool LiveOnly { get; set; }
    /// <summary>Played games with a model prediction in this view</summary>
    public int GameCount { get; set; }
    /// <summary>Of those, games whose prediction was saved before puck drop</summary>
    public int LiveGameCount { get; set; }
    public double? ModelLogLoss { get; set; }
    public double? ModelAccuracy { get; set; }
    public List<CalibrationBinVM> Calibration { get; set; } = [];
    public List<LogLossPointVM> LogLossByDate { get; set; } = [];
    /// <summary>The model against each market on the games that market priced</summary>
    public List<SourceComparisonVM> Sources { get; set; } = [];
    public ClosingLineVM? ClosingLine { get; set; }
}

/// <summary>Games whose home win probability fell in [Low, High)</summary>
public class CalibrationBinVM
{
    public double Low { get; set; }
    public double High { get; set; }
    public int Games { get; set; }
    public double MeanPredicted { get; set; }
    public double HomeWinRate { get; set; }
}

/// <summary>Season-to-date average log loss after a Central-time game day</summary>
public class LogLossPointVM
{
    public DateTime Date { get; set; }
    public int Games { get; set; }
    public double Model { get; set; }
    /// <summary>Average of the bookmakers' morning prices, on the games they priced; null before the first</summary>
    public double? Market { get; set; }
}

public class SourceComparisonVM
{
    public string Name { get; set; } = string.Empty;
    public int Games { get; set; }
    public double LogLoss { get; set; }
    public double Accuracy { get; set; }
    /// <summary>The model on the same games</summary>
    public double ModelLogLoss { get; set; }
    public double ModelAccuracy { get; set; }
}

/// <summary>
/// Closing line value: did the market move from its morning price toward the model by puck drop?
/// Uses games with a morning and closing price from the same bookmakers.
/// </summary>
public class ClosingLineVM
{
    public int Games { get; set; }
    /// <summary>Games where the model was closer to the closing price than the morning price was</summary>
    public int ModelCloser { get; set; }
    /// <summary>Games where the close moved toward the model's side of the morning price</summary>
    public int MovedTowardModel { get; set; }
    /// <summary>Games where it moved away</summary>
    public int MovedAwayFromModel { get; set; }
    /// <summary>Average move in the model's direction, in probability (0.01 = one percentage point)</summary>
    public double AverageMoveTowardModel { get; set; }
}
