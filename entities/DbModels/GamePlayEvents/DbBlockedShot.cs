using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbBlockedShot : IDbGameEvent
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
        public int blockingPlayerTeamId { get; set; }
        public int blockingPlayerId { get; set; }
        public int shooterPlayerId { get; set; }
        public int xCoordinate { get; set; }
        public int yCoordinate { get; set; }
        public Zone zone { get; set; }
        public BlockType blockType { get; set; }
        [ForeignKey("shooterPlayerId")]
        public DbPlayer? shooterPlayer { get; set; }
        [ForeignKey("blockingPlayerId")]
        public DbPlayer? blockingPlayer { get; set; }
        [ForeignKey("gameId")]
        public DbGameRaw? game { get; set; }
        [ForeignKey("blockingPlayerTeamId")]
        public DbTeam? blockingTeam { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbBlockedShot blockedShotEvent)
            {
                id = blockedShotEvent.id;
                gameId = blockedShotEvent.gameId;
                typeCode = blockedShotEvent.typeCode;
                sortOrder = blockedShotEvent.typeCode;
                situationCode = blockedShotEvent.situationCode;
                periodNumber = blockedShotEvent.periodNumber;
                periodType = blockedShotEvent.periodType;
                eventTypeName = blockedShotEvent.eventTypeName;
                homeTeamDefendingSide = blockedShotEvent.homeTeamDefendingSide;
                secondsIntoPeriod = blockedShotEvent.secondsIntoPeriod;
                secondsLeftInPeriod = blockedShotEvent.secondsLeftInPeriod;
                blockingPlayerTeamId = blockedShotEvent.blockingPlayerTeamId;
                blockingPlayerId = blockedShotEvent.blockingPlayerId;
                shooterPlayerId = blockedShotEvent.shooterPlayerId;
                xCoordinate = blockedShotEvent.xCoordinate;
                yCoordinate = blockedShotEvent.yCoordinate;
                zone = blockedShotEvent.zone;
                blockType = blockedShotEvent.blockType;
                shooterPlayer = blockedShotEvent.shooterPlayer;
                blockingPlayer = blockedShotEvent.blockingPlayer;
                game = blockedShotEvent.game;
                blockingTeam = blockedShotEvent.blockingTeam;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}