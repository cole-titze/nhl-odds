using Entities.Types.Enums;

namespace Entities.Models.GamePlayEvents;

public class Penalty : IGameEvent
{
    public int Id { get; set; }
    public int TypeCode { get; set; }
    public int SortOrder { get; set; }
    public int SituationCode { get; set; }
    public int PeriodNumber { get; set; }
    public PeriodType PeriodType { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public HomeTeamDefendingSide HomeTeamDefendingSide { get; set; }
    public int SecondsIntoPeriod { get; set; }
    public int SecondsLeftInPeriod { get; set; }
    public int CommittedByPlayerTeamId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public int? CommittedByPlayerId { get; set; }
    public int? DrawnByPlayerId { get; set; }
    public int? ServedByPlayerId { get; set; }
    public Zone Zone { get; set; }
    public int Duration { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public PenaltySeverity PenaltySeverity { get; set; }
}