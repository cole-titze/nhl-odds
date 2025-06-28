using System.ComponentModel.DataAnnotations.Schema;

using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbFaceoff : IDbGameEvent
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
    public int WinningTeamId { get; set; }
    public int WinningPlayerId { get; set; }
    public int LosingPlayerId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }
    [ForeignKey(nameof(WinningPlayerId))]
    public DbPlayer? WinningPlayer { get; set; }
    [ForeignKey(nameof(LosingPlayerId))]
    public DbPlayer? LosingPlayer { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    [ForeignKey(nameof(WinningTeamId))]
    public DbTeam? WinningTeam { get; set; }
    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbFaceoff faceoffEvent)
        {
            Id = faceoffEvent.Id;
            GameId = faceoffEvent.GameId;
            TypeCode = faceoffEvent.TypeCode;
            SortOrder = faceoffEvent.SortOrder;
            SituationCode = faceoffEvent.SituationCode;
            PeriodNumber = faceoffEvent.PeriodNumber;
            PeriodType = faceoffEvent.PeriodType;
            EventTypeName = faceoffEvent.EventTypeName;
            HomeTeamDefendingSide = faceoffEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = faceoffEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = faceoffEvent.SecondsLeftInPeriod;
            WinningTeamId = faceoffEvent.WinningTeamId;
            WinningPlayerId = faceoffEvent.WinningPlayerId;
            LosingPlayerId = faceoffEvent.LosingPlayerId;
            XCoordinate = faceoffEvent.XCoordinate;
            YCoordinate = faceoffEvent.YCoordinate;
            Zone = faceoffEvent.Zone;
            WinningPlayer = faceoffEvent.WinningPlayer;
            LosingPlayer = faceoffEvent.LosingPlayer;
            Game = faceoffEvent.Game;
            WinningTeam = faceoffEvent.WinningTeam;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}