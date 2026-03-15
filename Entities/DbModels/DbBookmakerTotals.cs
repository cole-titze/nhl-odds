using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

[Table("BookmakerTotals")]
public class DbBookmakerTotals
{
    public int GameId { get; set; }
    [MaxLength(100)]
    public string BookmakerName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string BookmakerKey { get; set; } = string.Empty;
    public double OverUnderPoint { get; set; }
    public int OverPrice { get; set; }
    public int UnderPrice { get; set; }
    public DateTime FetchedDateUTC { get; set; }
    public DateTime BookmakerLastUpdate { get; set; }
    public DateTime MarketLastUpdate { get; set; }
    [MaxLength(100)]
    public string OddsApiGameId { get; set; } = string.Empty;
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }

    public bool IsEquivalentTo(DbBookmakerTotals? other)
    {
        if (other == null) return false;
        return GameId == other.GameId
            && BookmakerName == other.BookmakerName
            && BookmakerKey == other.BookmakerKey
            && OverUnderPoint == other.OverUnderPoint
            && OverPrice == other.OverPrice
            && UnderPrice == other.UnderPrice
            && OddsApiGameId == other.OddsApiGameId;
    }

    public void Clone(DbBookmakerTotals other)
    {
        BookmakerKey = other.BookmakerKey;
        OverUnderPoint = other.OverUnderPoint;
        OverPrice = other.OverPrice;
        UnderPrice = other.UnderPrice;
        FetchedDateUTC = other.FetchedDateUTC;
        BookmakerLastUpdate = other.BookmakerLastUpdate;
        MarketLastUpdate = other.MarketLastUpdate;
        OddsApiGameId = other.OddsApiGameId;
    }
}
