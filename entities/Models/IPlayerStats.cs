namespace Entities.Models
{
    public interface IPlayerStats
    {
        public int playerId { get; set; }
        public int gameId { get; set; }
        public string name { get; set; }
        public int timeOnIceSeconds { get; set; }
        public POSITION position { get; set; }
        public double GetPlayerValue();
    }
}