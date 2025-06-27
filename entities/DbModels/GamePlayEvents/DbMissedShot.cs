using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbMissedShot : IDbGameEvent
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
        public ShotType shotType { get; set; }
        public int secondsIntoPeriod { get; set; }
        public int secondsLeftInPeriod { get; set; }
        public int shootingTeamId { get; set; }
        public int shootingPlayerId { get; set; }
        public int? goalieId { get; set; }
        public int xCoordinate { get; set; }
        public int yCoordinate { get; set; }
        public Zone zone { get; set; }
        public MissedShotType missType { get; set; }
        [ForeignKey("shootingPlayerId")]
        public DbPlayer? shootingPlayer { get; set; }
        [ForeignKey("goalieId")]
        public DbPlayer? goaliePlayer { get; set; }
        [ForeignKey("gameId")]
        public DbGameRaw? game { get; set; }
        [ForeignKey("shootingTeamId")]
        public DbTeam? shootingTeam { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbMissedShot missedShotEvent)
            {
                id = missedShotEvent.id;
                gameId = missedShotEvent.gameId;
                typeCode = missedShotEvent.typeCode;
                sortOrder = missedShotEvent.sortOrder;
                situationCode = missedShotEvent.situationCode;
                periodNumber = missedShotEvent.periodNumber;
                periodType = missedShotEvent.periodType;
                eventTypeName = missedShotEvent.eventTypeName;
                homeTeamDefendingSide = missedShotEvent.homeTeamDefendingSide;
                shotType = missedShotEvent.shotType;
                secondsIntoPeriod = missedShotEvent.secondsIntoPeriod;
                secondsLeftInPeriod = missedShotEvent.secondsLeftInPeriod;
                shootingTeamId = missedShotEvent.shootingTeamId;
                shootingPlayerId = missedShotEvent.shootingPlayerId;
                goalieId = missedShotEvent.goalieId;
                xCoordinate = missedShotEvent.xCoordinate;
                yCoordinate = missedShotEvent.yCoordinate;
                zone = missedShotEvent.zone;
                missType = missedShotEvent.missType;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}