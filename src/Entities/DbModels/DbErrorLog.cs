using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

[Table("ErrorLog")]
public class DbErrorLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateTime TimestampUTC { get; set; }
    public int? GameId { get; set; }
    public int? SeasonStartYear { get; set; }
    [MaxLength(500)]
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    [MaxLength(250)]
    public string Source { get; set; } = string.Empty;
}