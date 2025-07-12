using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapStoppageEvent
{
    /// <summary>
    /// Maps a stoppage event from the response to a Stoppage object.
    /// </summary>
    /// <param name="responseGameEvent">The event response from the NHL api</param>
    /// <returns>The stoppage event</returns>
    public static Stoppage Map(dynamic responseGameEvent)
    {
        string timeInPeriod = responseGameEvent.timeInPeriod;
        string timeLeftInPeriod = responseGameEvent.timeRemaining;

        return new Stoppage
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
            StoppageType = StoppageTypeParser.ParseFromString((string)responseGameEvent.details.reason),
            StoppageDetails = StoppageDetailsParser.ParseFromString(responseGameEvent.details.secondaryReason as string ?? "")
        };
    }
}