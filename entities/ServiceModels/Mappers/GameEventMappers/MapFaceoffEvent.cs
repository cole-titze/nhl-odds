using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapFaceoffEvent
{
    /// <summary>
    ///   Maps a faceoff event from the response to a Faceoff object.
    /// </summary>
    /// <param name="responseGameEvent">The event response from the NHL api</param>
    /// <returns>The mapped faceoff event</returns>
    public static Faceoff Map(dynamic responseGameEvent)
    {
        string timeInPeriod = responseGameEvent.timeInPeriod;
        string timeLeftInPeriod = responseGameEvent.timeRemaining;

        return new Faceoff
        {
            Id = (int)responseGameEvent.eventId,
            TypeCode = (int)responseGameEvent.typeCode,
            SortOrder = (int)responseGameEvent.sortOrder,
            SituationCode = SituationCodeParser.ParseFromString((string)responseGameEvent.situationCode),
            PeriodNumber = (int)responseGameEvent.periodDescriptor.number,
            PeriodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
            EventTypeName = (string)responseGameEvent.typeDescKey,
            HomeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string?)responseGameEvent.homeTeamDefendingSide),
            SecondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
            SecondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            WinningTeamId = responseGameEvent.details.eventOwnerTeamId,
            WinningPlayerId = responseGameEvent.details.winningPlayerId,
            LosingPlayerId = responseGameEvent.details.losingPlayerId,
            XCoordinate = responseGameEvent.details.xCoord,
            YCoordinate = responseGameEvent.details.yCoord,
            Zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
        };
    }
}