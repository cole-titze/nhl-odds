namespace Entities.DbModels;

public class DbBookmakerOddsResponse
{
    public int Id { get; set; }
    public DateTime FetchedDateUTC { get; set; }
    public string RawJson { get; set; } = string.Empty;
}
