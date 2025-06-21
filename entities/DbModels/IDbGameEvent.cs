namespace Entities.DbModels
{
    public interface IDbGameEvent
    {
        public int id { get; set; }
        public int gameId { get; set; }
        public int period { get; set; }
        public int situationCode { get; set; }
        public int typeCode { get; set; }
        public int sortOrder { get; set; }
        public string eventTypeName { get; set; }
        public DateTime timeInPeriod { get; set; }
        public DateTime timeLeftInGame { get; set; }
    }
}