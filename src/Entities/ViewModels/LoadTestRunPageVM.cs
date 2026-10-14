namespace Entities.ViewModels;

public class LoadTestRunPageVM
{
    public string Page { get; set; } = string.Empty;
    public int Requests { get; set; }
    public double RequestsPerSecond { get; set; }
    public double P50Ms { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }
    public double MaxMs { get; set; }
    public int Http5xx { get; set; }
    public int Http429 { get; set; }
    public int OtherErrors { get; set; }
}