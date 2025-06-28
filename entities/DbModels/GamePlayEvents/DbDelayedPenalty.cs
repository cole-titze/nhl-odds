using System.ComponentModel.DataAnnotations.Schema;

using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbDelayedPenalty : IDbGameEvent
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
    public int PenaltyTeamId { get; set; }
    [ForeignKey(nameof(PenaltyTeamId))]
    public DbTeam? PenaltyTeam { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbDelayedPenalty delayedPenaltyEvent)
        {
            Id = delayedPenaltyEvent.Id;
            GameId = delayedPenaltyEvent.GameId;
            TypeCode = delayedPenaltyEvent.TypeCode;
            SortOrder = delayedPenaltyEvent.SortOrder;
            SituationCode = delayedPenaltyEvent.SituationCode;
            PeriodNumber = delayedPenaltyEvent.PeriodNumber;
            PeriodType = delayedPenaltyEvent.PeriodType;
            EventTypeName = delayedPenaltyEvent.EventTypeName;
            HomeTeamDefendingSide = delayedPenaltyEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = delayedPenaltyEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = delayedPenaltyEvent.SecondsLeftInPeriod;
            PenaltyTeamId = delayedPenaltyEvent.PenaltyTeamId;
            PenaltyTeam = delayedPenaltyEvent.PenaltyTeam;
            Game = delayedPenaltyEvent.Game;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}