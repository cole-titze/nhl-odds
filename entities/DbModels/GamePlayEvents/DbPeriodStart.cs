using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbPeriodStart : IDbGameEvent
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
        [ForeignKey(nameof(gameId))]
        public DbGameRaw? game { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbPeriodStart periodStartEvent)
            {
                id = periodStartEvent.id;
                gameId = periodStartEvent.gameId;
                typeCode = periodStartEvent.typeCode;
                sortOrder = periodStartEvent.sortOrder;
                situationCode = periodStartEvent.situationCode;
                periodNumber = periodStartEvent.periodNumber;
                periodType = periodStartEvent.periodType;
                eventTypeName = periodStartEvent.eventTypeName;
                homeTeamDefendingSide = periodStartEvent.homeTeamDefendingSide;
                secondsIntoPeriod = periodStartEvent.secondsIntoPeriod;
                secondsLeftInPeriod = periodStartEvent.secondsLeftInPeriod;
                game = periodStartEvent.game;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}