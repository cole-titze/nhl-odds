using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbFaceoff : IDbGameEvent
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
        public int winningTeamId { get; set; }
        public int winningPlayerId { get; set; }
        public int losingPlayerId { get; set; }
        public int xCoordinate { get; set; }
        public int yCoordinate { get; set; }
        public Zone zone { get; set; }
        [ForeignKey("winningPlayerId")]
        public DbPlayer? winningPlayer { get; set; }
        [ForeignKey("losingPlayerId")]
        public DbPlayer? losingPlayer { get; set; }
        [ForeignKey("gameId")]
        public DbGameRaw? game { get; set; }
        [ForeignKey("winningTeamId")]
        public DbTeam? winningTeam { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbFaceoff faceoffEvent)
            {
                id = faceoffEvent.id;
                gameId = faceoffEvent.gameId;
                typeCode = faceoffEvent.typeCode;
                sortOrder = faceoffEvent.sortOrder;
                situationCode = faceoffEvent.situationCode;
                periodNumber = faceoffEvent.periodNumber;
                periodType = faceoffEvent.periodType;
                eventTypeName = faceoffEvent.eventTypeName;
                homeTeamDefendingSide = faceoffEvent.homeTeamDefendingSide;
                secondsIntoPeriod = faceoffEvent.secondsIntoPeriod;
                secondsLeftInPeriod = faceoffEvent.secondsLeftInPeriod;
                winningTeamId = faceoffEvent.winningTeamId;
                winningPlayerId = faceoffEvent.winningPlayerId;
                losingPlayerId = faceoffEvent.losingPlayerId;
                xCoordinate = faceoffEvent.xCoordinate;
                yCoordinate = faceoffEvent.yCoordinate;
                zone = faceoffEvent.zone;
                winningPlayer = faceoffEvent.winningPlayer;
                losingPlayer = faceoffEvent.losingPlayer;
                game = faceoffEvent.game;
                winningTeam = faceoffEvent.winningTeam;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}