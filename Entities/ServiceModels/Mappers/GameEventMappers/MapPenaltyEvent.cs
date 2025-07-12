using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapPenaltyEvent
{
    /// <summary>
    /// Maps a penalty event
    /// </summary>
    /// <param name="responseGameEvent">The event response from the NHL api</param>
    /// <returns>The penalty event</returns>
    public static Penalty Map(dynamic responseGameEvent)
    {
        string timeInPeriod = responseGameEvent.timeInPeriod;
        string timeLeftInPeriod = responseGameEvent.timeRemaining;

        return new Penalty
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
            PenaltyType = PenaltyTypeParser.ParseFromString((string)responseGameEvent.details.descKey),
            PenaltySeverity = PenaltySeverityParser.ParseFromString((string)responseGameEvent.details.typeCode),
            CommittedByPlayerTeamId = (int)responseGameEvent.details.eventOwnerTeamId,
            DrawnByPlayerId = (int?)responseGameEvent.details.drawnByPlayerId,
            ServedByPlayerId = (int?)responseGameEvent.details.servedByPlayerId,
            CommittedByPlayerId = (int?)responseGameEvent.details.committedByPlayerId,
            Zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
            XCoordinate = (int?)responseGameEvent.details.xCoord,
            YCoordinate = (int?)responseGameEvent.details.yCoord,
            Duration = (int)responseGameEvent.details.duration,
        };
    }
}