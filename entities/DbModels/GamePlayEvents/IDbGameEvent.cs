using Entities.Types.Enums;

namespace Entities.DbModels;

public interface IDbGameEvent
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int PeriodNumber { get; set; }
    public PeriodType PeriodType { get; set; }
    // Players on ice (Ex. "1551" means 1 goalie 5 players on both sides)
    public int SituationCode { get; set; }
    public int TypeCode { get; set; }
    public int SortOrder { get; set; }
    public string EventTypeName { get; set; }
    public HomeTeamDefendingSide HomeTeamDefendingSide { get; set; }
    public int SecondsIntoPeriod { get; set; }
    public int SecondsLeftInPeriod { get; set; }
    public void Clone(IDbGameEvent gameOdds);
}