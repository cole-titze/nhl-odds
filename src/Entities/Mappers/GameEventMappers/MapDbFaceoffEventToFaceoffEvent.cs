using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbFaceoffEventToFaceoffEvent
{
    public static Faceoff Map(DbFaceoff db)
    {
        return new Faceoff
        {
            Id = db.Id,
            TypeCode = db.TypeCode,
            SortOrder = db.SortOrder,
            SituationCode = db.SituationCode,
            PeriodNumber = db.PeriodNumber,
            PeriodType = db.PeriodType,
            EventTypeName = db.EventTypeName,
            HomeTeamDefendingSide = db.HomeTeamDefendingSide,
            SecondsIntoPeriod = db.SecondsIntoPeriod,
            SecondsLeftInPeriod = db.SecondsLeftInPeriod,
            WinningTeamId = db.WinningTeamId,
            WinningPlayerId = db.WinningPlayerId,
            LosingPlayerId = db.LosingPlayerId,
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            Zone = db.Zone
        };
    }

    public static IEnumerable<Faceoff> MapList(IEnumerable<DbFaceoff> dbEvents)
    {
        var events = new List<Faceoff>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}
