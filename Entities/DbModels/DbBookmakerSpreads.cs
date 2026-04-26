using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

[Table("BookmakerSpreads")]
public class DbBookmakerSpreads
{
    public int GameId { get; set; }
    [MaxLength(100)]
    public string BookmakerName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string BookmakerKey { get; set; } = string.Empty;
    public double HomePoint { get; set; }
    public int HomePrice { get; set; }
    public double AwayPoint { get; set; }
    public int AwayPrice { get; set; }
    public DateTime FetchedDateUTC { get; set; }
    public DateTime BookmakerLastUpdate { get; set; }
    public DateTime MarketLastUpdate { get; set; }
    [MaxLength(100)]
    public string OddsApiGameId { get; set; } = string.Empty;
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }

    public bool IsEquivalentTo(DbBookmakerSpreads? other)
    {
        if (other == null) return false;
        return GameId == other.GameId
            && BookmakerName == other.BookmakerName
            && BookmakerKey == other.BookmakerKey
            && HomePoint == other.HomePoint
            && HomePrice == other.HomePrice
            && AwayPoint == other.AwayPoint
            && AwayPrice == other.AwayPrice
            && OddsApiGameId == other.OddsApiGameId;
    }

    public void Clone(DbBookmakerSpreads other)
    {
        BookmakerKey = other.BookmakerKey;
        HomePoint = other.HomePoint;
        HomePrice = other.HomePrice;
        AwayPoint = other.AwayPoint;
        AwayPrice = other.AwayPrice;
        FetchedDateUTC = other.FetchedDateUTC;
        BookmakerLastUpdate = other.BookmakerLastUpdate;
        MarketLastUpdate = other.MarketLastUpdate;
        OddsApiGameId = other.OddsApiGameId;
    }
}