using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types.Enums;

namespace Entities.DbModels.GamePlayEvents;

public class DbHit : IDbGameEvent
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
    public int HittingPlayerTeamId { get; set; }
    public int HittingPlayerId { get; set; }
    public int HitteePlayerId { get; set; }
    public int? XCoordinate { get; set; }
    public int? YCoordinate { get; set; }
    public Zone Zone { get; set; }

    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }

    [ForeignKey(nameof(HittingPlayerId))]
    public DbPlayer? HittingPlayer { get; set; }

    [ForeignKey(nameof(HitteePlayerId))]
    public DbPlayer? HitteePlayer { get; set; }
    [ForeignKey(nameof(HittingPlayerTeamId))]
    public DbTeam? HittingPlayerTeam { get; set; }

    public void Clone(IDbGameEvent gameEvent)
    {
        if (gameEvent is DbHit hitEvent)
        {
            Id = hitEvent.Id;
            GameId = hitEvent.GameId;
            TypeCode = hitEvent.TypeCode;
            SortOrder = hitEvent.SortOrder;
            SituationCode = hitEvent.SituationCode;
            PeriodNumber = hitEvent.PeriodNumber;
            PeriodType = hitEvent.PeriodType;
            EventTypeName = hitEvent.EventTypeName;
            HomeTeamDefendingSide = hitEvent.HomeTeamDefendingSide;
            SecondsIntoPeriod = hitEvent.SecondsIntoPeriod;
            SecondsLeftInPeriod = hitEvent.SecondsLeftInPeriod;
            HittingPlayerTeamId = hitEvent.HittingPlayerTeamId;
            HittingPlayerId = hitEvent.HittingPlayerId;
            HitteePlayerId = hitEvent.HitteePlayerId;
            XCoordinate = hitEvent.XCoordinate;
            YCoordinate = hitEvent.YCoordinate;
            Zone = hitEvent.Zone;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
    public bool IsEquivalentTo(IDbGameEvent? other)
    {
        if (other == null || other is not DbHit hit)
            return false;

        return Id == hit.Id
            && GameId == hit.GameId
            && TypeCode == hit.TypeCode
            && SortOrder == hit.SortOrder
            && SituationCode == hit.SituationCode
            && PeriodNumber == hit.PeriodNumber
            && PeriodType == hit.PeriodType
            && EventTypeName == hit.EventTypeName
            && HomeTeamDefendingSide == hit.HomeTeamDefendingSide
            && SecondsIntoPeriod == hit.SecondsIntoPeriod
            && SecondsLeftInPeriod == hit.SecondsLeftInPeriod
            && HittingPlayerTeamId == hit.HittingPlayerTeamId
            && HittingPlayerId == hit.HittingPlayerId
            && HitteePlayerId == hit.HitteePlayerId
            && XCoordinate == hit.XCoordinate
            && YCoordinate == hit.YCoordinate
            && Zone == hit.Zone;

    }
}