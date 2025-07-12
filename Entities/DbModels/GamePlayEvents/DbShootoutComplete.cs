using System.ComponentModel.DataAnnotations.Schema;
using Entities.Models.GamePlayEvents;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbShootoutComplete : IDbGameEvent
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
        if (gameEvent is DbPeriodStart shootoutCompleteEvent)
        {
            Id = shootoutCompleteEvent.Id;
            GameId = shootoutCompleteEvent.GameId;
            TypeCode = shootoutCompleteEvent.TypeCode;
            SortOrder = shootoutCompleteEvent.SortOrder;
            SituationCode = shootoutCompleteEvent.SituationCode;
            PeriodNumber = shootoutCompleteEvent.PeriodNumber;
            PeriodType = shootoutCompleteEvent.PeriodType;
            EventTypeName = shootoutCompleteEvent.EventTypeName;
            HomeTeamDefendingSide = shootoutCompleteEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = shootoutCompleteEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = shootoutCompleteEvent.SecondsLeftInPeriod;
            Game = shootoutCompleteEvent.Game;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
    public bool IsEquivalentTo(IDbGameEvent? other)
    {
        if (other == null || other is not DbShootoutComplete)
            return false;

        return Id == other.Id
            && GameId == other.GameId
            && TypeCode == other.TypeCode
            && SortOrder == other.SortOrder
            && SituationCode == other.SituationCode
            && PeriodNumber == other.PeriodNumber
            && PeriodType == other.PeriodType
            && EventTypeName == other.EventTypeName
            && HomeTeamDefendingSide == other.HomeTeamDefendingSide
            && SecondsIntoPeriod == other.SecondsIntoPeriod
            && SecondsLeftInPeriod == other.SecondsLeftInPeriod;
    }
}