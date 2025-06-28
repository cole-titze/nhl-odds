using System.ComponentModel.DataAnnotations.Schema;

using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbGameEnd : IDbGameEvent
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
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbGameEnd gameEndEvent)
        {
            Id = gameEndEvent.Id;
            GameId = gameEndEvent.GameId;
            TypeCode = gameEndEvent.TypeCode;
            SortOrder = gameEndEvent.SortOrder;
            SituationCode = gameEndEvent.SituationCode;
            PeriodNumber = gameEndEvent.PeriodNumber;
            PeriodType = gameEndEvent.PeriodType;
            EventTypeName = gameEndEvent.EventTypeName;
            HomeTeamDefendingSide = gameEndEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = gameEndEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = gameEndEvent.SecondsLeftInPeriod;
            Game = gameEndEvent.Game;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}