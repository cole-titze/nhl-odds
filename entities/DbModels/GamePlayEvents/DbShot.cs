using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbShot : IDbGameEvent
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
    public int ShootingTeamId { get; set; }
    public int ShootingPlayerId { get; set; }
    public int? GoalieId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }
    public ShotType ShotType { get; set; }
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
        if (gameEvent is DbShot shotEvent)
        {
            Id = shotEvent.Id;
            GameId = shotEvent.GameId;
            TypeCode = shotEvent.TypeCode;
            SortOrder = shotEvent.SortOrder;
            SituationCode = shotEvent.SituationCode;
            PeriodNumber = shotEvent.PeriodNumber;
            PeriodType = shotEvent.PeriodType;
            EventTypeName = shotEvent.EventTypeName;
            HomeTeamDefendingSide = shotEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = shotEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = shotEvent.SecondsLeftInPeriod;
            ShootingTeamId = shotEvent.ShootingTeamId;
            ShootingPlayerId = shotEvent.ShootingPlayerId;
            GoalieId = shotEvent.GoalieId;
            XCoordinate = shotEvent.XCoordinate;
            YCoordinate = shotEvent.YCoordinate;
            Zone = shotEvent.Zone;
            ShotType = shotEvent.ShotType;
            ShootingPlayer = shotEvent.ShootingPlayer;
            GoaliePlayer = shotEvent.GoaliePlayer;
            Game = shotEvent.Game;
            ShootingTeam = shotEvent.ShootingTeam;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}