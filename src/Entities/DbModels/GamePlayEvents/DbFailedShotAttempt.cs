using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbFailedShotAttempt : IDbGameEvent
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
    public ShotType ShotType { get; set; }
    public int SecondsIntoPeriod { get; set; }
    public int SecondsLeftInPeriod { get; set; }
    public int ShootingTeamId { get; set; }
    public int ShootingPlayerId { get; set; }
    public int? GoalieId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }
    [ForeignKey(nameof(ShootingPlayerId))]
    public DbPlayer? ShootingPlayer { get; set; }
    [ForeignKey(nameof(GoalieId))]
    public DbPlayer? GoaliePlayer { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    [ForeignKey(nameof(ShootingTeamId))]
    public DbTeam? ShootingTeam { get; set; }
    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbFailedShotAttempt failedShotAttemptEvent)
        {
            Id = failedShotAttemptEvent.Id;
            GameId = failedShotAttemptEvent.GameId;
            TypeCode = failedShotAttemptEvent.TypeCode;
            SortOrder = failedShotAttemptEvent.SortOrder;
            SituationCode = failedShotAttemptEvent.SituationCode;
            PeriodNumber = failedShotAttemptEvent.PeriodNumber;
            PeriodType = failedShotAttemptEvent.PeriodType;
            EventTypeName = failedShotAttemptEvent.EventTypeName;
            HomeTeamDefendingSide = failedShotAttemptEvent.HomeTeamDefendingSide;
            ShotType = failedShotAttemptEvent.ShotType;
            SecondsIntoPeriod = failedShotAttemptEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = failedShotAttemptEvent.SecondsLeftInPeriod;
            ShootingTeamId = failedShotAttemptEvent.ShootingTeamId;
            ShootingPlayerId = failedShotAttemptEvent.ShootingPlayerId;
            GoalieId = failedShotAttemptEvent.GoalieId;
            XCoordinate = failedShotAttemptEvent.XCoordinate;
            YCoordinate = failedShotAttemptEvent.YCoordinate;
            Zone = failedShotAttemptEvent.Zone;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
    public bool IsEquivalentTo(IDbGameEvent? other)
    {
        if (other == null || other is not DbFailedShotAttempt failedShotAttempt)
            return false;

        return Id == failedShotAttempt.Id
            && GameId == failedShotAttempt.GameId
            && TypeCode == failedShotAttempt.TypeCode
            && SortOrder == failedShotAttempt.SortOrder
            && SituationCode == failedShotAttempt.SituationCode
            && PeriodNumber == failedShotAttempt.PeriodNumber
            && PeriodType == failedShotAttempt.PeriodType
            && EventTypeName == failedShotAttempt.EventTypeName
            && HomeTeamDefendingSide == failedShotAttempt.HomeTeamDefendingSide
            && ShotType == failedShotAttempt.ShotType
            && SecondsIntoPeriod == failedShotAttempt.SecondsIntoPeriod
            && SecondsLeftInPeriod == failedShotAttempt.SecondsLeftInPeriod
            && ShootingTeamId == failedShotAttempt.ShootingTeamId
            && ShootingPlayerId == failedShotAttempt.ShootingPlayerId
            && GoalieId == failedShotAttempt.GoalieId
            && XCoordinate == failedShotAttempt.XCoordinate
            && YCoordinate == failedShotAttempt.YCoordinate
            && Zone == failedShotAttempt.Zone;
    }
}