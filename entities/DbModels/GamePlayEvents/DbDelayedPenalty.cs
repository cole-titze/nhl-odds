using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbDelayedPenalty : IDbGameEvent
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
        public int penaltyTeamId { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbDelayedPenalty delayedPenaltyEvent)
            {
                id = delayedPenaltyEvent.id;
                gameId = delayedPenaltyEvent.gameId;
                typeCode = delayedPenaltyEvent.typeCode;
                sortOrder = delayedPenaltyEvent.sortOrder;
                situationCode = delayedPenaltyEvent.situationCode;
                periodNumber = delayedPenaltyEvent.periodNumber;
                periodType = delayedPenaltyEvent.periodType;
                eventTypeName = delayedPenaltyEvent.eventTypeName;
                homeTeamDefendingSide = delayedPenaltyEvent.homeTeamDefendingSide;
                secondsIntoPeriod = delayedPenaltyEvent.secondsIntoPeriod;
                secondsLeftInPeriod = delayedPenaltyEvent.secondsLeftInPeriod;
                penaltyTeamId = delayedPenaltyEvent.penaltyTeamId;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}