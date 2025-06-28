using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbTakeaway : IDbGameEvent
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
    public int TakeawayPlayerTeamId { get; set; }
    public int TakeawayPlayerId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }
    [ForeignKey(nameof(TakeawayPlayerTeamId))]
    public DbTeam? TakeawayPlayerTeam { get; set; }
    [ForeignKey(nameof(TakeawayPlayerId))]
    public DbPlayer? TakeawayPlayer { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }

    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbTakeaway takeawayEvent)
        {
            Id = takeawayEvent.Id;
            GameId = takeawayEvent.GameId;
            TypeCode = takeawayEvent.TypeCode;
            SortOrder = takeawayEvent.SortOrder;
            SituationCode = takeawayEvent.SituationCode;
            PeriodNumber = takeawayEvent.PeriodNumber;
            PeriodType = takeawayEvent.PeriodType;
            EventTypeName = takeawayEvent.EventTypeName;
            HomeTeamDefendingSide = takeawayEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = takeawayEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = takeawayEvent.SecondsLeftInPeriod;
            TakeawayPlayerTeamId = takeawayEvent.TakeawayPlayerTeamId;
            TakeawayPlayerId = takeawayEvent.TakeawayPlayerId;
            XCoordinate = takeawayEvent.XCoordinate;
            YCoordinate = takeawayEvent.YCoordinate;
            Zone = takeawayEvent.Zone;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}