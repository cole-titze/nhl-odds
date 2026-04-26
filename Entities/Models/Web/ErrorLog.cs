namespace Entities.Models.Web;

public class ErrorLog
{
    public int Id { get; set; }
    public DateTime TimestampUTC { get; set; }
    public int? GameId { get; set; }
    public int? SeasonStartYear { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}