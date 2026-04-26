using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

[Table("JobStatus")]
public class DbJobStatus
{
    [Key]
    [MaxLength(100)]
    public string JobName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Status { get; set; } = "idle";
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Error { get; set; }
}