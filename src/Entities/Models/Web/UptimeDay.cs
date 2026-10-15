namespace Entities.Models.Web;

/// <summary>One Central Time day of the public site's uptime probe (scheduler/k8s/save-uptime.sh).</summary>
public class UptimeDay
{
    public DateOnly Day { get; set; }
    /// <summary>Probes the day should have had (today: so far); the ones missing are time with no data.</summary>
    public int ExpectedProbes { get; set; }
    public int Probes { get; set; }
    public int SuccessfulProbes { get; set; }
    public double? AvgResponseMs { get; set; }
}
