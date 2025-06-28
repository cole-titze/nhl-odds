using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents
{
    public class DbGiveaway : IDbGameEvent
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
        public int giveawayPlayerTeamId { get; set; }
        public int giveawayPlayerId { get; set; }
        public int? xCoordinate { get; set; }
        public int? yCoordinate { get; set; }
        public Zone zone { get; set; }
        [ForeignKey(nameof(giveawayPlayerId))]
        public DbPlayer? giveawayPlayer { get; set; }
        [ForeignKey(nameof(gameId))]
        public DbGameRaw? game { get; set; }
        [ForeignKey(nameof(giveawayPlayerTeamId))]
        public DbTeam? giveawayPlayerTeam { get; set; }
        public void Clone(IDbGameEvent gameEvent)
        {
            if (gameEvent is DbGiveaway giveawayEvent)
            {
                id = giveawayEvent.id;
                gameId = giveawayEvent.gameId;
                typeCode = giveawayEvent.typeCode;
                sortOrder = giveawayEvent.sortOrder;
                situationCode = giveawayEvent.situationCode;
                periodNumber = giveawayEvent.periodNumber;
                periodType = giveawayEvent.periodType;
                eventTypeName = giveawayEvent.eventTypeName;
                homeTeamDefendingSide = giveawayEvent.homeTeamDefendingSide;
                secondsIntoPeriod = giveawayEvent.secondsIntoPeriod;
                secondsLeftInPeriod = giveawayEvent.secondsLeftInPeriod;
                giveawayPlayerTeamId = giveawayEvent.giveawayPlayerTeamId;
                giveawayPlayerId = giveawayEvent.giveawayPlayerId;
                xCoordinate = giveawayEvent.xCoordinate;
                yCoordinate = giveawayEvent.yCoordinate;
                zone = giveawayEvent.zone;
                giveawayPlayer = giveawayEvent.giveawayPlayer;
                game = giveawayEvent.game;
                giveawayPlayerTeam = giveawayEvent.giveawayPlayerTeam;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to Clone.");
            }
        }
    }
}