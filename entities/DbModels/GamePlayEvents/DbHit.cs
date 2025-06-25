using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbHit : IDbGameEvent
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
        public int hittingPlayerTeamId { get; set; }
        public int hittingPlayerId { get; set; }
        public int hitteePlayerId { get; set; }
        public int xCoordinate { get; set; }
        public int yCoordinate { get; set; }
        public Zone zone { get; set; }

        [ForeignKey(nameof(gameId))]
        public DbGameRaw? Game { get; set; }

        [ForeignKey(nameof(hittingPlayerId))]
        public DbPlayer? HittingPlayer { get; set; }

        [ForeignKey(nameof(hitteePlayerId))]
        public DbPlayer? HitteePlayer { get; set; }
        [ForeignKey(nameof(hittingPlayerTeamId))]
        public DbTeam? hittingPlayerTeam { get; set; }

        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbHit hitEvent)
            {
                id = hitEvent.id;
                gameId = hitEvent.gameId;
                typeCode = hitEvent.typeCode;
                sortOrder = hitEvent.sortOrder;
                situationCode = hitEvent.situationCode;
                periodNumber = hitEvent.periodNumber;
                periodType = hitEvent.periodType;
                eventTypeName = hitEvent.eventTypeName;
                homeTeamDefendingSide = hitEvent.homeTeamDefendingSide;
                secondsIntoPeriod = hitEvent.secondsIntoPeriod;
                secondsLeftInPeriod = hitEvent.secondsLeftInPeriod;
                hittingPlayerTeamId = hitEvent.hittingPlayerTeamId;
                hittingPlayerId = hitEvent.hittingPlayerId;
                hitteePlayerId = hitEvent.hitteePlayerId;
                xCoordinate = hitEvent.xCoordinate;
                yCoordinate = hitEvent.yCoordinate;
                zone = hitEvent.zone;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}