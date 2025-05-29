using Entities.Models;

namespace Entities.DbModels
{
    public interface IDbGamePlayerStats
    {
        public int playerId { get; set; }
        public int gameId { get; set; }
        public int timeOnIceSeconds { get; set; }
        public POSITION position { get; set; }
        public DbGameRaw game { get; set; }
        public DbPlayer player { get; set; }
        public bool IsValid();
        public void Clone(IDbGamePlayerStats gamePlayerStats);
    }
}