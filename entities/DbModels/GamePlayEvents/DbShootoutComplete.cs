using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbShootoutComplete : IDbGameEvent
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
            if (gameEvent is DbPeriodStart shootoutCompleteEvent)
            {
                id = shootoutCompleteEvent.id;
                gameId = shootoutCompleteEvent.gameId;
                typeCode = shootoutCompleteEvent.typeCode;
                sortOrder = shootoutCompleteEvent.sortOrder;
                situationCode = shootoutCompleteEvent.situationCode;
                periodNumber = shootoutCompleteEvent.periodNumber;
                periodType = shootoutCompleteEvent.periodType;
                eventTypeName = shootoutCompleteEvent.eventTypeName;
                homeTeamDefendingSide = shootoutCompleteEvent.homeTeamDefendingSide;
                secondsIntoPeriod = shootoutCompleteEvent.secondsIntoPeriod;
                secondsLeftInPeriod = shootoutCompleteEvent.secondsLeftInPeriod;
                game = shootoutCompleteEvent.game;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}