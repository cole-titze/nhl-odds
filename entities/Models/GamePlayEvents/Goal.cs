using Entities.Types.Enums;

namespace Entities.Models.GamePlayEvents
{
    public class Goal : IGameEvent
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
        public int? xCoordinate { get; set; }
        public int? yCoordinate { get; set; }
        public Zone zone { get; set; }
        public ShotType shotType { get; set; }
        public int scoringPlayerTeamId { get; set; }
        public int? assistOnePlayerId { get; set; }
        public int? assistTwoPlayerId { get; set; }
        public int scoringPlayerId { get; set; }
        public int? goalieId { get; set; }
        public string highlightClipSharingUrl { get; set; } = string.Empty;
        public int highlightClipId { get; set; }
        public int discreetClipId { get; set; }
        public string pptReplayUrl { get; set; } = string.Empty;
    }
}