using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbPenalty : IDbGameEvent
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
    public int CommittedByPlayerTeamId { get; set; }
    public int? DrawnByPlayerId { get; set; }
    public int? CommittedByPlayerId { get; set; }
    public int? ServedByPlayerId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }
    public int Duration { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public PenaltySeverity PenaltySeverity { get; set; }

    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }

    [ForeignKey(nameof(DrawnByPlayerId))]
    public DbPlayer? DrawnByPlayer { get; set; }

    [ForeignKey(nameof(CommittedByPlayerId))]
    public DbPlayer? CommittedByPlayer { get; set; }

    [ForeignKey(nameof(CommittedByPlayerTeamId))]
    public DbTeam? CommittedByPlayerTeam { get; set; }
    [ForeignKey(nameof(ServedByPlayerId))]
    public DbTeam? ServedByPlayer { get; set; }

    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbPenalty penaltyEvent)
        {
            Id = penaltyEvent.Id;
            GameId = penaltyEvent.GameId;
            TypeCode = penaltyEvent.TypeCode;
            SortOrder = penaltyEvent.SortOrder;
            SituationCode = penaltyEvent.SituationCode;
            PeriodNumber = penaltyEvent.PeriodNumber;
            PeriodType = penaltyEvent.PeriodType;
            EventTypeName = penaltyEvent.EventTypeName;
            HomeTeamDefendingSide = penaltyEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = penaltyEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = penaltyEvent.SecondsLeftInPeriod;
            CommittedByPlayerTeamId = penaltyEvent.CommittedByPlayerTeamId;
            DrawnByPlayerId = penaltyEvent.DrawnByPlayerId;
            CommittedByPlayerId = penaltyEvent.CommittedByPlayerId;
            ServedByPlayerId = penaltyEvent.ServedByPlayerId;
            XCoordinate = penaltyEvent.XCoordinate;
            YCoordinate = penaltyEvent.YCoordinate;
            Zone = penaltyEvent.Zone;
            Duration = penaltyEvent.Duration;
            PenaltyType = penaltyEvent.PenaltyType;
            PenaltySeverity = penaltyEvent.PenaltySeverity;
            Game = penaltyEvent.Game;
            DrawnByPlayer = penaltyEvent.DrawnByPlayer;
            CommittedByPlayer = penaltyEvent.CommittedByPlayer;
            CommittedByPlayerTeam = penaltyEvent.CommittedByPlayerTeam;
            ServedByPlayer = penaltyEvent.ServedByPlayer;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
    public bool IsEquivalentTo(IDbGameEvent? other)
    {
        if (other == null || other is not DbPenalty penalty)
            return false;

        return Id == penalty.Id
            && GameId == penalty.GameId
            && TypeCode == penalty.TypeCode
            && SortOrder == penalty.SortOrder
            && SituationCode == penalty.SituationCode
            && PeriodNumber == penalty.PeriodNumber
            && PeriodType == penalty.PeriodType
            && EventTypeName == penalty.EventTypeName
            && HomeTeamDefendingSide == penalty.HomeTeamDefendingSide
            && SecondsIntoPeriod == penalty.SecondsIntoPeriod
            && SecondsLeftInPeriod == penalty.SecondsLeftInPeriod
            && CommittedByPlayerTeamId == penalty.CommittedByPlayerTeamId
            && DrawnByPlayerId == penalty.DrawnByPlayerId
            && CommittedByPlayerId == penalty.CommittedByPlayerId
            && ServedByPlayerId == penalty.ServedByPlayerId
            && XCoordinate == penalty.XCoordinate
            && YCoordinate == penalty.YCoordinate
            && Zone == penalty.Zone
            && Duration == penalty.Duration
            && PenaltyType == penalty.PenaltyType
            && PenaltySeverity == penalty.PenaltySeverity;
    }
}