using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbTakeaway : IDbGameEvent
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
        public int takeawayPlayerTeamId { get; set; }
        public int takeawayPlayerId { get; set; }
        public int? xCoordinate { get; set; }
        public int? yCoordinate { get; set; }
        public Zone zone { get; set; }
        [ForeignKey(nameof(takeawayPlayerTeamId))]
        public DbTeam? takeawayPlayerTeam { get; set; }
        [ForeignKey(nameof(takeawayPlayerId))]
        public DbPlayer? takeawayPlayer { get; set; }
        [ForeignKey(nameof(gameId))]
        public DbGameRaw? game { get; set; }

        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbTakeaway takeawayEvent)
            {
                id = takeawayEvent.id;
                gameId = takeawayEvent.gameId;
                typeCode = takeawayEvent.typeCode;
                sortOrder = takeawayEvent.sortOrder;
                situationCode = takeawayEvent.situationCode;
                periodNumber = takeawayEvent.periodNumber;
                periodType = takeawayEvent.periodType;
                eventTypeName = takeawayEvent.eventTypeName;
                homeTeamDefendingSide = takeawayEvent.homeTeamDefendingSide;
                secondsIntoPeriod = takeawayEvent.secondsIntoPeriod;
                secondsLeftInPeriod = takeawayEvent.secondsLeftInPeriod;
                takeawayPlayerTeamId = takeawayEvent.takeawayPlayerTeamId;
                takeawayPlayerId = takeawayEvent.takeawayPlayerId;
                xCoordinate = takeawayEvent.xCoordinate;
                yCoordinate = takeawayEvent.yCoordinate;
                zone = takeawayEvent.zone;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}