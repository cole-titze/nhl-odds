namespace Entities.ViewModels;

public class UptimeDayVM
{
    public DateOnly Day { get; set; }
    public int ExpectedProbes { get; set; }
    public int Probes { get; set; }
    public int SuccessfulProbes { get; set; }
    public double? AvgResponseMs { get; set; }
}
