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
        public int shooterTeamId { get; set; }
        public int shooterPlayerId { get; set; }
        public int goalieId { get; set; }
        public int xCoordinate { get; set; }
        public int yCoordinate { get; set; }
        public Zone zone { get; set; }
        public ShotType shotType { get; set; }
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
                shooterTeamId = shotEvent.shooterTeamId;
                shooterPlayerId = shotEvent.shooterPlayerId;
                goalieId = shotEvent.goalieId;
                xCoordinate = shotEvent.xCoordinate;
                yCoordinate = shotEvent.yCoordinate;
                zone = shotEvent.zone;
                shotType = shotEvent.shotType;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}