using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapShotEvent
{
    /// <summary>
    ///  Maps a shot event from the response to a ShotEvent object.
    /// </summary>
    /// <param name="responseGameEvent">The event response from the NHL api</param>
    /// <returns>The shot event</returns>
    public static Shot Map(dynamic responseGameEvent)
    {
        string timeInPeriod = responseGameEvent.timeInPeriod;
        string timeLeftInPeriod = responseGameEvent.timeRemaining;

        return new Shot
        {
            Id = (int)responseGameEvent.eventId,
            TypeCode = (int)responseGameEvent.typeCode,
            SortOrder = (int)responseGameEvent.sortOrder,
            SituationCode = int.Parse((string)responseGameEvent.situationCode ?? "1551"),
            PeriodNumber = (int)responseGameEvent.periodDescriptor.number,
            PeriodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
            EventTypeName = (string)responseGameEvent.typeDescKey,
            HomeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
            SecondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
            SecondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            ShotType = ShotTypeParser.ParseFromString((string)responseGameEvent.details.shotType),
            Zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
            ShooterTeamId = responseGameEvent.details.eventOwnerTeamId,
            ShooterPlayerId = responseGameEvent.details.shootingPlayerId,
            GoalieId = (int?)responseGameEvent.details.goalieInNetId,
            XCoordinate = (int?)responseGameEvent.details.xCoord,
            YCoordinate = (int?)responseGameEvent.details.yCoord,
        };
    }
}