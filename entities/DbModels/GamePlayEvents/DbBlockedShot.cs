using System.ComponentModel.DataAnnotations.Schema;

using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbBlockedShot : IDbGameEvent
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
    public int BlockingPlayerTeamId { get; set; }
    public int BlockingPlayerId { get; set; }
    public int ShooterPlayerId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }
    public BlockType BlockType { get; set; }
    [ForeignKey(nameof(ShooterPlayerId))]
    public DbPlayer? ShooterPlayer { get; set; }
    [ForeignKey(nameof(BlockingPlayerId))]
    public DbPlayer? BlockingPlayer { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    [ForeignKey(nameof(BlockingPlayerTeamId))]
    public DbTeam? BlockingTeam { get; set; }
    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbBlockedShot blockedShotEvent)
        {
            Id = blockedShotEvent.Id;
            GameId = blockedShotEvent.GameId;
            TypeCode = blockedShotEvent.TypeCode;
            SortOrder = blockedShotEvent.TypeCode;
            SituationCode = blockedShotEvent.SituationCode;
            PeriodNumber = blockedShotEvent.PeriodNumber;
            PeriodType = blockedShotEvent.PeriodType;
            EventTypeName = blockedShotEvent.EventTypeName;
            HomeTeamDefendingSide = blockedShotEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = blockedShotEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = blockedShotEvent.SecondsLeftInPeriod;
            BlockingPlayerTeamId = blockedShotEvent.BlockingPlayerTeamId;
            BlockingPlayerId = blockedShotEvent.BlockingPlayerId;
            ShooterPlayerId = blockedShotEvent.ShooterPlayerId;
            XCoordinate = blockedShotEvent.XCoordinate;
            YCoordinate = blockedShotEvent.YCoordinate;
            Zone = blockedShotEvent.Zone;
            BlockType = blockedShotEvent.BlockType;
            ShooterPlayer = blockedShotEvent.ShooterPlayer;
            BlockingPlayer = blockedShotEvent.BlockingPlayer;
            Game = blockedShotEvent.Game;
            BlockingTeam = blockedShotEvent.BlockingTeam;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}