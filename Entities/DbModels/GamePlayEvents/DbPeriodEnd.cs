using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbPeriodEnd : IDbGameEvent
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
        if (gameEvent is DbPeriodStart periodEndEvent)
        {
            Id = periodEndEvent.Id;
            GameId = periodEndEvent.GameId;
            TypeCode = periodEndEvent.TypeCode;
            SortOrder = periodEndEvent.SortOrder;
            SituationCode = periodEndEvent.SituationCode;
            PeriodNumber = periodEndEvent.PeriodNumber;
            PeriodType = periodEndEvent.PeriodType;
            EventTypeName = periodEndEvent.EventTypeName;
            HomeTeamDefendingSide = periodEndEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = periodEndEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = periodEndEvent.SecondsLeftInPeriod;
            Game = periodEndEvent.Game;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
    public bool IsEquivalentTo(IDbGameEvent? other)
    {
        if (other == null || other is not DbPeriodEnd periodEnd)
            return false;

        return Id == periodEnd.Id
            && GameId == periodEnd.GameId
            && TypeCode == periodEnd.TypeCode
            && SortOrder == periodEnd.SortOrder
            && SituationCode == periodEnd.SituationCode
            && PeriodNumber == periodEnd.PeriodNumber
            && PeriodType == periodEnd.PeriodType
            && EventTypeName == periodEnd.EventTypeName
            && HomeTeamDefendingSide == periodEnd.HomeTeamDefendingSide
            && SecondsIntoPeriod == periodEnd.SecondsIntoPeriod
            && SecondsLeftInPeriod == periodEnd.SecondsLeftInPeriod;
    }
}