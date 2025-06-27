using Entities.Types.Enums;

namespace Entities.Models.GamePlayEvents
{
    public class Penalty : IGameEvent
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
        public int committedByPlayerTeamId { get; set; }
        public int? xCoordinate { get; set; }
        public int? yCoordinate { get; set; }
        public int? committedByPlayerId { get; set; }
        public int? drawnByPlayerId { get; set; }
        public int? servedByPlayerId { get; set; }
        public Zone zone { get; set; }
        public int duration { get; set; }
        public PenaltyType penaltyType { get; set; }
        public PenaltySeverity penaltySeverity { get; set; }
    }
}