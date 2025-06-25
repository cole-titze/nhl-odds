using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbGameEnd : IDbGameEvent
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
        [ForeignKey("gameId")]
        public DbGameRaw? game { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbGameEnd gameEndEvent)
            {
                id = gameEndEvent.id;
                gameId = gameEndEvent.gameId;
                typeCode = gameEndEvent.typeCode;
                sortOrder = gameEndEvent.sortOrder;
                situationCode = gameEndEvent.situationCode;
                periodNumber = gameEndEvent.periodNumber;
                periodType = gameEndEvent.periodType;
                eventTypeName = gameEndEvent.eventTypeName;
                homeTeamDefendingSide = gameEndEvent.homeTeamDefendingSide;
                secondsIntoPeriod = gameEndEvent.secondsIntoPeriod;
                secondsLeftInPeriod = gameEndEvent.secondsLeftInPeriod;
                game = gameEndEvent.game;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}