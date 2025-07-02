using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbGoal : IDbGameEvent
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
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }

    [ForeignKey(nameof(ScoringPlayerId))]
    public DbPlayer? ScoringPlayer { get; set; }

    [ForeignKey(nameof(AssistOnePlayerId))]
    public DbPlayer? AssistOnePlayer { get; set; }

    [ForeignKey(nameof(AssistTwoPlayerId))]
    public DbPlayer? AssistTwoPlayer { get; set; }
    [ForeignKey(nameof(ScoringPlayerTeamId))]
    public DbTeam? ScoringTeam { get; set; }

    [ForeignKey(nameof(GoalieId))]
    public DbPlayer? Goalie { get; set; }

    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbGoal goalEvent)
        {
            Id = goalEvent.Id;
            GameId = goalEvent.GameId;
            TypeCode = goalEvent.TypeCode;
            SortOrder = goalEvent.SortOrder;
            SituationCode = goalEvent.SituationCode;
            PeriodNumber = goalEvent.PeriodNumber;
            PeriodType = goalEvent.PeriodType;
            EventTypeName = goalEvent.EventTypeName;
            HomeTeamDefendingSide = goalEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = goalEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = goalEvent.SecondsLeftInPeriod;
            XCoordinate = goalEvent.XCoordinate;
            YCoordinate = goalEvent.YCoordinate;
            Zone = goalEvent.Zone;
            ShotType = goalEvent.ShotType;
            ScoringPlayerTeamId = goalEvent.ScoringPlayerTeamId;
            AssistOnePlayerId = goalEvent.AssistOnePlayerId;
            AssistTwoPlayerId = goalEvent.AssistTwoPlayerId;
            ScoringPlayerId = goalEvent.ScoringPlayerId;
            GoalieId = goalEvent.GoalieId;
            HighlightClipSharingUrl = goalEvent.HighlightClipSharingUrl;
            HighlightClipId = goalEvent.HighlightClipId;
            DiscreetClipId = goalEvent.DiscreetClipId;
            PptReplayUrl = goalEvent.PptReplayUrl;
            Game = goalEvent.Game;
            ScoringPlayer = goalEvent.ScoringPlayer;
            AssistOnePlayer = goalEvent.AssistOnePlayer;
            AssistTwoPlayer = goalEvent.AssistTwoPlayer;
            Goalie = goalEvent.Goalie;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
    public bool IsEquivalentTo(IDbGameEvent? other)
    {
        if (other == null || other is not DbGoal goal)
            return false;

        return Id == goal.Id
            && GameId == goal.GameId
            && TypeCode == goal.TypeCode
            && SortOrder == goal.SortOrder
            && SituationCode == goal.SituationCode
            && PeriodNumber == goal.PeriodNumber
            && PeriodType == goal.PeriodType
            && EventTypeName == goal.EventTypeName
            && HomeTeamDefendingSide == goal.HomeTeamDefendingSide
            && SecondsIntoPeriod == goal.SecondsIntoPeriod
            && SecondsLeftInPeriod == goal.SecondsLeftInPeriod
            && XCoordinate == goal.XCoordinate
            && YCoordinate == goal.YCoordinate
            && Zone == goal.Zone
            && ShotType == goal.ShotType
            && ScoringPlayerTeamId == goal.ScoringPlayerTeamId
            && AssistOnePlayerId == goal.AssistOnePlayerId
            && AssistTwoPlayerId == goal.AssistTwoPlayerId
            && ScoringPlayerId == goal.ScoringPlayerId
            && GoalieId == goal.GoalieId
            && HighlightClipSharingUrl == goal.HighlightClipSharingUrl
            && HighlightClipId == goal.HighlightClipId
            && DiscreetClipId == goal.DiscreetClipId
            && PptReplayUrl == goal.PptReplayUrl;
    }
}