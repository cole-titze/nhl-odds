using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbPenalty : IDbGameEvent
    {
        public int id { get; set; }
        public int gameId { get; set; }
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
        public int drawnByPlayerId { get; set; }
        public int committedByPlayerId { get; set; }
        public int xCoordinate { get; set; }
        public int yCoordinate { get; set; }
        public Zone zone { get; set; }
        public int duration { get; set; }
        public PenaltyType penaltyType { get; set; }
        public PenaltySeverity penaltySeverity { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbPenalty penaltyEvent)
            {
                id = penaltyEvent.id;
                gameId = penaltyEvent.gameId;
                typeCode = penaltyEvent.typeCode;
                sortOrder = penaltyEvent.sortOrder;
                situationCode = penaltyEvent.situationCode;
                periodNumber = penaltyEvent.periodNumber;
                periodType = penaltyEvent.periodType;
                eventTypeName = penaltyEvent.eventTypeName;
                homeTeamDefendingSide = penaltyEvent.homeTeamDefendingSide;
                secondsIntoPeriod = penaltyEvent.secondsIntoPeriod;
                secondsLeftInPeriod = penaltyEvent.secondsLeftInPeriod;
                committedByPlayerTeamId = penaltyEvent.committedByPlayerTeamId;
                drawnByPlayerId = penaltyEvent.drawnByPlayerId;
                committedByPlayerId = penaltyEvent.committedByPlayerId;
                xCoordinate = penaltyEvent.xCoordinate;
                yCoordinate = penaltyEvent.yCoordinate;
                zone = penaltyEvent.zone;
                duration = penaltyEvent.duration;
                penaltyType = penaltyEvent.penaltyType;
                penaltySeverity = penaltyEvent.penaltySeverity;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}