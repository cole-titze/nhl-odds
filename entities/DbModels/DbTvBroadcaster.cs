using System.ComponentModel.DataAnnotations;

namespace Entities.DbModels
{
    public class DbTvBroadcaster
    {
        [Key]
        public int id { get; set; }
        public string networkName { get; set; } = string.Empty;
        public string marketAbbreviation { get; set; } = string.Empty;
        public int sequenceNumber { get; set; }
        public string countryCode { get; set; } = string.Empty;
        public void Clone(DbTvBroadcaster dbTvBroadcaster)
        {
            id = dbTvBroadcaster.id;
            networkName = dbTvBroadcaster.networkName;
            marketAbbreviation = dbTvBroadcaster.marketAbbreviation;
            sequenceNumber = dbTvBroadcaster.sequenceNumber;
            countryCode = dbTvBroadcaster.countryCode;
        }
    }
}