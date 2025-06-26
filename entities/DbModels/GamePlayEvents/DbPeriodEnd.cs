using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbPeriodEnd : IDbGameEvent
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
            if (gameEvent is DbPeriodStart periodEndEvent)
            {
                id = periodEndEvent.id;
                gameId = periodEndEvent.gameId;
                typeCode = periodEndEvent.typeCode;
                sortOrder = periodEndEvent.sortOrder;
                situationCode = periodEndEvent.situationCode;
                periodNumber = periodEndEvent.periodNumber;
                periodType = periodEndEvent.periodType;
                eventTypeName = periodEndEvent.eventTypeName;
                homeTeamDefendingSide = periodEndEvent.homeTeamDefendingSide;
                secondsIntoPeriod = periodEndEvent.secondsIntoPeriod;
                secondsLeftInPeriod = periodEndEvent.secondsLeftInPeriod;
                game = periodEndEvent.game;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}