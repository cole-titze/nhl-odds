using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbMissedShot : IDbGameEvent
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
    public MissedShotType MissType { get; set; }
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
        if (gameEvent is DbMissedShot missedShotEvent)
        {
            Id = missedShotEvent.Id;
            GameId = missedShotEvent.GameId;
            TypeCode = missedShotEvent.TypeCode;
            SortOrder = missedShotEvent.SortOrder;
            SituationCode = missedShotEvent.SituationCode;
            PeriodNumber = missedShotEvent.PeriodNumber;
            PeriodType = missedShotEvent.PeriodType;
            EventTypeName = missedShotEvent.EventTypeName;
            HomeTeamDefendingSide = missedShotEvent.HomeTeamDefendingSide;
            ShotType = missedShotEvent.ShotType;
            SecondsIntoPeriod = missedShotEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = missedShotEvent.SecondsLeftInPeriod;
            ShootingTeamId = missedShotEvent.ShootingTeamId;
            ShootingPlayerId = missedShotEvent.ShootingPlayerId;
            GoalieId = missedShotEvent.GoalieId;
            XCoordinate = missedShotEvent.XCoordinate;
            YCoordinate = missedShotEvent.YCoordinate;
            Zone = missedShotEvent.Zone;
            MissType = missedShotEvent.MissType;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}