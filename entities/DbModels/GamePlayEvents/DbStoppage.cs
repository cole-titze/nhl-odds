using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbStoppage : IDbGameEvent
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
        public StoppageType stoppageType { get; set; }
        public StoppageDetails stoppageDetails { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbStoppage stoppageEvent)
            {
                id = stoppageEvent.id;
                gameId = stoppageEvent.gameId;
                typeCode = stoppageEvent.typeCode;
                sortOrder = stoppageEvent.sortOrder;
                situationCode = stoppageEvent.situationCode;
                periodNumber = stoppageEvent.periodNumber;
                periodType = stoppageEvent.periodType;
                eventTypeName = stoppageEvent.eventTypeName;
                homeTeamDefendingSide = stoppageEvent.homeTeamDefendingSide;
                secondsIntoPeriod = stoppageEvent.secondsIntoPeriod;
                secondsLeftInPeriod = stoppageEvent.secondsLeftInPeriod;
                stoppageType = stoppageEvent.stoppageType;
                stoppageDetails = stoppageEvent.stoppageDetails;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}