using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers
{
    public static class MapGameEventsResponseToGameEvents
    {
        public static GameEvents Map(dynamic response)
        {
            var gameEvents = new GameEvents();
            foreach (var responseGameEvent in response.plays)
            {
                var gameEvent = GetGameEvent(responseGameEvent);
                gameEvents.Add(gameEvent);
            }

            return gameEvents;
        }

        /// <summary>
        ///  Maps a single game event from the response to a GameEvent object.
        /// </summary>
        /// <param name="responseGameEvent">A game event from the nhl api</param>
        /// <returns>The Game Event</returns>
        private static IGameEvent GetGameEvent(dynamic responseGameEvent)
        {
            var eventType = EventTypeParser.Parse((string)responseGameEvent.typeDescKey);
            switch (eventType)
            {
                case EventType.PeriodStart:
                    return GetPeriodStartEvent(responseGameEvent);
                case EventType.Faceoff:
                    return GetFaceoffEvent(responseGameEvent);
                case EventType.Shot:
                    return GetShotEvent(responseGameEvent);
                case EventType.Stoppage:
                    return GetStoppageEvent(responseGameEvent);
                case "Penalty":
                    return new Penalty(responseGameEvent);
                case "Faceoff":
                    return new Faceoff(responseGameEvent);
                case "Blocked Shot":
                    return new BlockedShot(responseGameEvent);
                case "Missed Shot":
                    return new MissedShot(responseGameEvent);
                default:
                    return new UnknownEvent(responseGameEvent);
            }
        }

        /// <summary>
        /// Maps a stoppage event from the response to a Stoppage object.
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The stoppage event</returns>
        private static Stoppage GetStoppageEvent(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new Stoppage
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = int.Parse((string)responseGameEvent.situationCode),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = responseGameEvent.periodDescriptor.periodType,
                eventTypeName = responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
                stoppageType = StoppageTypeParser.ParseFromString((string)responseGameEvent.details.reason),
                stoppageDetails = StoppageDetailsParser.ParseFromString(responseGameEvent.details.secondaryReason as string ?? "")
            };
        }

        /// <summary>
        ///  Maps a shot event from the response to a ShotEvent object.
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The shot event</returns>
        private static Shot GetShotEvent(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new Shot
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = int.Parse((string)responseGameEvent.situationCode),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
                eventTypeName = (string)responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
                shotType = ShotTypeParser.ParseFromString((string)responseGameEvent.details.shotType),
                zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
                shooterTeamId = responseGameEvent.details.eventOwnerTeamId,
                shooterPlayerId = responseGameEvent.details.shootingPlayerId,
                goalieId = responseGameEvent.details.goalieInNetId,
                xCoordinate = responseGameEvent.details.xCoord,
                yCoordinate = responseGameEvent.details.yCoord,
            };
        }

        /// <summary>
        ///   Maps a faceoff event from the response to a Faceoff object.
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The mapped faceoff event</returns>
        private static Faceoff GetFaceoffEvent(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new Faceoff
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = int.Parse((string)responseGameEvent.situationCode),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = responseGameEvent.periodDescriptor.periodType,
                eventTypeName = responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
                winningTeamId = responseGameEvent.details.eventOwnerTeamId,
                winningPlayerId = responseGameEvent.details.winningPlayerId,
                losingPlayerId = responseGameEvent.details.losingPlayerId,
                xCoordinate = responseGameEvent.details.xCoord,
                yCoordinate = responseGameEvent.details.yCoord,
                zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
            };
        }

        /// <summary>
        ///  Maps a period start event from the response to a PeriodStart object.
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The period start event</returns>
        private static PeriodStart GetPeriodStartEvent(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new PeriodStart
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = int.Parse((string)responseGameEvent.situationCode),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
                eventTypeName = (string)responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            };
        }
    }
}