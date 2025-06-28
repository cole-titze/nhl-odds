using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapMissedShotEvent
{
    /// <summary>
    /// Maps a blocked shot event
    /// </summary>
    /// <param name="responseGameEvent">The event response from the NHL api</param>
    /// <returns>The blocked shot event</returns>
    public static MissedShot Map(dynamic responseGameEvent)
    {
        string timeInPeriod = responseGameEvent.timeInPeriod;
        string timeLeftInPeriod = responseGameEvent.timeRemaining;

        return new MissedShot
        {
            Id = (int)responseGameEvent.eventId,
            TypeCode = (int)responseGameEvent.typeCode,
            SortOrder = (int)responseGameEvent.sortOrder,
            SituationCode = SituationCodeParser.ParseFromString((string)responseGameEvent.situationCode),
            PeriodNumber = (int)responseGameEvent.periodDescriptor.number,
            PeriodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
            EventTypeName = (string)responseGameEvent.typeDescKey,
            HomeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
            SecondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
            SecondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            ShotType = ShotTypeParser.ParseFromString((string)responseGameEvent.details.shotType),
            ShootingTeamId = (int)responseGameEvent.details.eventOwnerTeamId,
            ShootingPlayerId = (int)responseGameEvent.details.shootingPlayerId,
            GoalieId = (int?)responseGameEvent.details.goalieInNetId,
            Zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
            XCoordinate = (int?)responseGameEvent.details.xCoord,
            YCoordinate = (int?)responseGameEvent.details.yCoord,
            MissType = MissedShotTypeParser.ParseFromString((string)responseGameEvent.details.reason)
        };
    }
}