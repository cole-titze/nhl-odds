using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbPeriodStart : IDbGameEvent
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
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbPeriodStart periodStartEvent)
        {
            Id = periodStartEvent.Id;
            GameId = periodStartEvent.GameId;
            TypeCode = periodStartEvent.TypeCode;
            SortOrder = periodStartEvent.SortOrder;
            SituationCode = periodStartEvent.SituationCode;
            PeriodNumber = periodStartEvent.PeriodNumber;
            PeriodType = periodStartEvent.PeriodType;
            EventTypeName = periodStartEvent.EventTypeName;
            HomeTeamDefendingSide = periodStartEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = periodStartEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = periodStartEvent.SecondsLeftInPeriod;
            Game = periodStartEvent.Game;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}