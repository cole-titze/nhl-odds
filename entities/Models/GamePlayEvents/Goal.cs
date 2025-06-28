using Entities.Types.Enums;

namespace Entities.Models.GamePlayEvents;

public class Goal : IGameEvent
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
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }
    public ShotType ShotType { get; set; }
    public int ScoringPlayerTeamId { get; set; }
    public int? AssistOnePlayerId { get; set; }
    public int? AssistTwoPlayerId { get; set; }
    public int ScoringPlayerId { get; set; }
    public int? GoalieId { get; set; }
    public string HighlightClipSharingUrl { get; set; } = string.Empty;
    public int HighlightClipId { get; set; }
    public int DiscreetClipId { get; set; }
    public string PptReplayUrl { get; set; } = string.Empty;
}