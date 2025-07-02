using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbGiveaway : IDbGameEvent
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int TypeCode { get; set; }
    public int SortOrder { get; set; }
    public int SituationCode { get; set; }
    public int PeriodNumber { get; set; }
    public PeriodType PeriodType { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public HomeTeamDefendingSide HomeTeamDefendingSide { get; set; }
    public int SecondsIntoPeriod { get; set; }
    public int SecondsLeftInPeriod { get; set; }
    public int GiveawayPlayerTeamId { get; set; }
    public int GiveawayPlayerId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }
    [ForeignKey(nameof(GiveawayPlayerId))]
    public DbPlayer? GiveawayPlayer { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    [ForeignKey(nameof(GiveawayPlayerTeamId))]
    public DbTeam? GiveawayPlayerTeam { get; set; }
    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbGiveaway giveawayEvent)
        {
            Id = giveawayEvent.Id;
            GameId = giveawayEvent.GameId;
            TypeCode = giveawayEvent.TypeCode;
            SortOrder = giveawayEvent.SortOrder;
            SituationCode = giveawayEvent.SituationCode;
            PeriodNumber = giveawayEvent.PeriodNumber;
            PeriodType = giveawayEvent.PeriodType;
            EventTypeName = giveawayEvent.EventTypeName;
            HomeTeamDefendingSide = giveawayEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = giveawayEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = giveawayEvent.SecondsLeftInPeriod;
            GiveawayPlayerTeamId = giveawayEvent.GiveawayPlayerTeamId;
            GiveawayPlayerId = giveawayEvent.GiveawayPlayerId;
            XCoordinate = giveawayEvent.XCoordinate;
            YCoordinate = giveawayEvent.YCoordinate;
            Zone = giveawayEvent.Zone;
            GiveawayPlayer = giveawayEvent.GiveawayPlayer;
            Game = giveawayEvent.Game;
            GiveawayPlayerTeam = giveawayEvent.GiveawayPlayerTeam;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
    public bool IsEquivalentTo(IDbGameEvent? other)
    {
        if (other == null || other is not DbGiveaway giveaway)
            return false;

        return Id == giveaway.Id
            && GameId == giveaway.GameId
            && TypeCode == giveaway.TypeCode
            && SortOrder == giveaway.SortOrder
            && SituationCode == giveaway.SituationCode
            && PeriodNumber == giveaway.PeriodNumber
            && PeriodType == giveaway.PeriodType
            && EventTypeName == giveaway.EventTypeName
            && HomeTeamDefendingSide == giveaway.HomeTeamDefendingSide
            && SecondsIntoPeriod == giveaway.SecondsIntoPeriod
            && SecondsLeftInPeriod == giveaway.SecondsLeftInPeriod
            && GiveawayPlayerTeamId == giveaway.GiveawayPlayerTeamId
            && GiveawayPlayerId == giveaway.GiveawayPlayerId
            && XCoordinate == giveaway.XCoordinate
            && YCoordinate == giveaway.YCoordinate
            && Zone == giveaway.Zone;
    }
}