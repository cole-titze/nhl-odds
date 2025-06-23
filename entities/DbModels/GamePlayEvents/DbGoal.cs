using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbGoal : IDbGameEvent
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
        public int xCoordinate { get; set; }
        public int yCoordinate { get; set; }
        public Zone zone { get; set; }
        public ShotType shotType { get; set; }
        public int scoringPlayerTeamId { get; set; }
        public int assistOnePlayerId { get; set; }
        public int assistTwoPlayerId { get; set; }
        public int scoringPlayerId { get; set; }
        public int goalieId { get; set; }
        public string highlightClipSharingUrl { get; set; } = string.Empty;
        public int highlightClipId { get; set; }
        public int discreetClipId { get; set; }
        public string pptReplayUrl { get; set; } = string.Empty;
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbGoal goalEvent)
            {
                id = goalEvent.id;
                gameId = goalEvent.gameId;
                typeCode = goalEvent.typeCode;
                sortOrder = goalEvent.sortOrder;
                situationCode = goalEvent.situationCode;
                periodNumber = goalEvent.periodNumber;
                periodType = goalEvent.periodType;
                eventTypeName = goalEvent.eventTypeName;
                homeTeamDefendingSide = goalEvent.homeTeamDefendingSide;
                secondsIntoPeriod = goalEvent.secondsIntoPeriod;
                secondsLeftInPeriod = goalEvent.secondsLeftInPeriod;
                xCoordinate = goalEvent.xCoordinate;
                yCoordinate = goalEvent.yCoordinate;
                zone = goalEvent.zone;
                shotType = goalEvent.shotType;
                scoringPlayerTeamId = goalEvent.scoringPlayerTeamId;
                assistOnePlayerId = goalEvent.assistOnePlayerId;
                assistTwoPlayerId = goalEvent.assistTwoPlayerId;
                scoringPlayerId = goalEvent.scoringPlayerId;
                goalieId = goalEvent.goalieId;
                highlightClipSharingUrl = goalEvent.highlightClipSharingUrl;
                highlightClipId = goalEvent.highlightClipId;
                discreetClipId = goalEvent.discreetClipId;
                pptReplayUrl = goalEvent.pptReplayUrl;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}