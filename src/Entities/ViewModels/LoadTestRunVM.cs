namespace Entities.ViewModels;

public class LoadTestRunVM
{
    public int Id { get; set; }
    public string Preset { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int MaxVus { get; set; }
    public int RampUpSeconds { get; set; }
    public int HoldSeconds { get; set; }
    public int RampDownSeconds { get; set; }
    public double ThinkTimeMinSeconds { get; set; }
    public double ThinkTimeMaxSeconds { get; set; }
    public string PageMix { get; set; } = string.Empty;
    public DateTime StartedUTC { get; set; }
    public DateTime FinishedUTC { get; set; }
    public int Requests { get; set; }
    public double RequestsPerSecond { get; set; }
    public double P50Ms { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }
    public double MaxMs { get; set; }
    public int Http5xx { get; set; }
    public int Http429 { get; set; }
    public int OtherErrors { get; set; }
    public List<LoadTestRunPageVM> Pages { get; set; } = new();
}