using Entities.Types.Enums;

namespace Entities.Models.GamePlayEvents
{
    public class Giveaway : IGameEvent
    {
        public int id { get; set; }
        public int typeCode { get; set; }
        public int sortOrder { get; set; }
        public int situationCode { get; set; }
        public int periodNumber { get; set; }
        public PeriodType periodType { get; set; }
        public string eventTypeName { get; set; } = string.Empty;
        public HomeTeamDefendingSide homeTeamDefendingSide { get; set; }
        public int secondsIntoPeriod { get; set; }
        public int secondsLeftInPeriod { get; set; }
        public int giveawayPlayerTeamId { get; set; }
        public int giveawayPlayerId { get; set; }
        public int? xCoordinate { get; set; }
        public int? yCoordinate { get; set; }
        public Zone zone { get; set; }
    }
}