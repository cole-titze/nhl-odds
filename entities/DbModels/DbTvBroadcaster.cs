using System.ComponentModel.DataAnnotations;

namespace Entities.DbModels;

public class DbTvBroadcaster
{
    [Key]
    public int Id { get; set; }
    public string NetworkName { get; set; } = string.Empty;
    public string MarketAbbreviation { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public void Clone(DbTvBroadcaster dbTvBroadcaster)
    {
        Id = dbTvBroadcaster.Id;
        NetworkName = dbTvBroadcaster.NetworkName;
        MarketAbbreviation = dbTvBroadcaster.MarketAbbreviation;
        SequenceNumber = dbTvBroadcaster.SequenceNumber;
        CountryCode = dbTvBroadcaster.CountryCode;
    }
}