using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapFaceoffEventToDbFaceoffEvent
{
    public static DbFaceoff Map(Faceoff faceoffEvent, int gameId)
    {
        return new DbFaceoff
        {
            Id = faceoffEvent.Id,
            GameId = gameId,
            TypeCode = faceoffEvent.TypeCode,
            SortOrder = faceoffEvent.SortOrder,
            SituationCode = faceoffEvent.SituationCode,
            PeriodNumber = faceoffEvent.PeriodNumber,
            PeriodType = faceoffEvent.PeriodType,
            EventTypeName = faceoffEvent.EventTypeName,
            HomeTeamDefendingSide = faceoffEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = faceoffEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = faceoffEvent.SecondsLeftInPeriod,
            WinningTeamId = faceoffEvent.WinningTeamId,
            WinningPlayerId = faceoffEvent.WinningPlayerId,
            LosingPlayerId = faceoffEvent.LosingPlayerId,
            XCoordinate = faceoffEvent.XCoordinate,
            YCoordinate = faceoffEvent.YCoordinate,
            Zone = faceoffEvent.Zone
        };
    }

    public static IEnumerable<DbFaceoff> MapList(IEnumerable<Faceoff> faceoffEvents, int gameId)
    {
        var dbEvents = new List<DbFaceoff>();
        foreach (var faceoffEvent in faceoffEvents)
        {
            dbEvents.Add(Map(faceoffEvent, gameId));
        }

        return dbEvents;
    }
}