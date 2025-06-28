using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapTakeawayEvent
{
    /// <summary>
    /// Maps a giveaway event
    /// </summary>
    /// <param name="responseGameEvent">The event response from the NHL api</param>
    /// <returns>The giveaway event</returns>
    public static Takeaway Map(dynamic responseGameEvent)
    {
        string timeInPeriod = responseGameEvent.timeInPeriod;
        string timeLeftInPeriod = responseGameEvent.timeRemaining;

        return new Takeaway
        {
            Id = (int)responseGameEvent.eventId,
            TypeCode = (int)responseGameEvent.typeCode,
            SortOrder = (int)responseGameEvent.sortOrder,
            SituationCode = SituationCodeParser.ParseFromString((string)responseGameEvent.situationCode),
            PeriodNumber = (int)responseGameEvent.periodDescriptor.number,
            PeriodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
            EventTypeName = responseGameEvent.typeDescKey,
            HomeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
            SecondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
            SecondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            TakeawayPlayerTeamId = (int)responseGameEvent.details.eventOwnerTeamId,
            TakeawayPlayerId = (int)responseGameEvent.details.playerId,
            Zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
            XCoordinate = (int?)responseGameEvent.details.xCoord,
            YCoordinate = (int?)responseGameEvent.details.yCoord,
        };
    }
}