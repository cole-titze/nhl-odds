using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

[Table("UptimeDay")]
public class DbUptimeDay
{
    [Key]
    public DateOnly Day { get; set; }
    public int ExpectedProbes { get; set; }
    public int Probes { get; set; }
    public int SuccessfulProbes { get; set; }
    public double? AvgResponseMs { get; set; }
    public DateTime UpdatedUTC { get; set; }
}
