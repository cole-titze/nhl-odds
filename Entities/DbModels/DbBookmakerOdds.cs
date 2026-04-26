using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

[Table("BookmakerOdds")]
public class DbBookmakerOdds
{
    public int GameId { get; set; }
    [MaxLength(100)]
    public string BookmakerName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string BookmakerKey { get; set; } = string.Empty;
    public double HomeOdds { get; set; }
    public double AwayOdds { get; set; }
    public DateTime FetchedDateUTC { get; set; }
    public DateTime BookmakerLastUpdate { get; set; }
    public DateTime MarketLastUpdate { get; set; }
    [MaxLength(100)]
    public string OddsApiGameId { get; set; } = string.Empty;
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }

    public bool IsEquivalentTo(DbBookmakerOdds? other)
    {
        if (other == null) return false;
        return GameId == other.GameId
            && BookmakerName == other.BookmakerName
            && BookmakerKey == other.BookmakerKey
            && HomeOdds == other.HomeOdds
            && AwayOdds == other.AwayOdds
            && OddsApiGameId == other.OddsApiGameId;
    }

    public void Clone(DbBookmakerOdds other)
    {
        BookmakerKey = other.BookmakerKey;
        HomeOdds = other.HomeOdds;
        AwayOdds = other.AwayOdds;
        FetchedDateUTC = other.FetchedDateUTC;
        BookmakerLastUpdate = other.BookmakerLastUpdate;
        MarketLastUpdate = other.MarketLastUpdate;
        OddsApiGameId = other.OddsApiGameId;
    }
}