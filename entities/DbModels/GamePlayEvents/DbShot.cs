using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbShot : IDbGameEvent
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
        public int shootingTeamId { get; set; }
        public int shootingPlayerId { get; set; }
        public int goalieId { get; set; }
        public int? xCoordinate { get; set; }
        public int? yCoordinate { get; set; }
        public Zone zone { get; set; }
        public ShotType shotType { get; set; }
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
            if (gameEvent is DbShot shotEvent)
            {
                id = shotEvent.id;
                gameId = shotEvent.gameId;
                typeCode = shotEvent.typeCode;
                sortOrder = shotEvent.sortOrder;
                situationCode = shotEvent.situationCode;
                periodNumber = shotEvent.periodNumber;
                periodType = shotEvent.periodType;
                eventTypeName = shotEvent.eventTypeName;
                homeTeamDefendingSide = shotEvent.homeTeamDefendingSide;
                secondsIntoPeriod = shotEvent.secondsIntoPeriod;
                secondsLeftInPeriod = shotEvent.secondsLeftInPeriod;
                shootingTeamId = shotEvent.shootingTeamId;
                shootingPlayerId = shotEvent.shootingPlayerId;
                goalieId = shotEvent.goalieId;
                xCoordinate = shotEvent.xCoordinate;
                yCoordinate = shotEvent.yCoordinate;
                zone = shotEvent.zone;
                shotType = shotEvent.shotType;
                shootingPlayer = shotEvent.shootingPlayer;
                goaliePlayer = shotEvent.goaliePlayer;
                game = shotEvent.game;
                shootingTeam = shotEvent.shootingTeam;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}