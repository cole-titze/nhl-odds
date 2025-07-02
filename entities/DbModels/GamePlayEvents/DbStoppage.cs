using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbStoppage : IDbGameEvent
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
    public StoppageType StoppageType { get; set; }
    public StoppageDetails StoppageDetails { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbStoppage stoppageEvent)
        {
            Id = stoppageEvent.Id;
            GameId = stoppageEvent.GameId;
            TypeCode = stoppageEvent.TypeCode;
            SortOrder = stoppageEvent.SortOrder;
            SituationCode = stoppageEvent.SituationCode;
            PeriodNumber = stoppageEvent.PeriodNumber;
            PeriodType = stoppageEvent.PeriodType;
            EventTypeName = stoppageEvent.EventTypeName;
            HomeTeamDefendingSide = stoppageEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = stoppageEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = stoppageEvent.SecondsLeftInPeriod;
            StoppageType = stoppageEvent.StoppageType;
            StoppageDetails = stoppageEvent.StoppageDetails;
            Game = stoppageEvent.Game;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
    public bool IsEquivalentTo(IDbGameEvent? other)
    {
        if (other == null || other is not DbStoppage stoppage)
            return false;

        return Id == stoppage.Id
            && GameId == stoppage.GameId
            && TypeCode == stoppage.TypeCode
            && SortOrder == stoppage.SortOrder
            && SituationCode == stoppage.SituationCode
            && PeriodNumber == stoppage.PeriodNumber
            && PeriodType == stoppage.PeriodType
            && EventTypeName == stoppage.EventTypeName
            && HomeTeamDefendingSide == stoppage.HomeTeamDefendingSide
            && SecondsIntoPeriod == stoppage.SecondsIntoPeriod
            && SecondsLeftInPeriod == stoppage.SecondsLeftInPeriod
            && StoppageType == stoppage.StoppageType
            && StoppageDetails == stoppage.StoppageDetails;
    }
}