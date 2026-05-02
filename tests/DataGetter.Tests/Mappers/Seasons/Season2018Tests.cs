using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2018020001: MTL @ TOR
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2018020001
/// </summary>
[TestClass]
public class Season2018Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2018;
    protected override int    ExpectedHomeTeamId      => 10;
    protected override int    ExpectedAwayTeamId      => 8;
    protected override string ExpectedHomeTeamAbbr    => "TOR";
    protected override string ExpectedAwayTeamAbbr    => "MTL";
    protected override int    ExpectedHomeGoals        => 3;
    protected override int    ExpectedAwayGoals        => 2;

    protected override string BoxscoreJson => """
        {
          "id": 2018020001,
          "season": 20182019,
          "startTimeUTC": "2018-10-03T23:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 4,
            "periodType": "OT",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 10,
            "abbrev": "TOR"
          },
          "awayTeam": {
            "id": 8,
            "abbrev": "MTL"
          },
          "tvBroadcasts": [
            {
              "id": 282,
              "market": "N",
              "countryCode": "CA",
              "network": "SN",
              "sequenceNumber": 107
            },
            {
              "id": 33,
              "market": "A",
              "countryCode": "CA",
              "network": "RDS",
              "sequenceNumber": 145
            }
          ],
          "venue": {
            "default": "Scotiabank Arena"
          },
          "venueLocation": {
            "default": "Toronto"
          }
        }
        """;

    protected override string RightRailJson => """
        {
          "linescore": {
            "byPeriod": [
              {
                "periodDescriptor": {
                  "number": 1,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 1,
                "home": 1
              },
              {
                "periodDescriptor": {
                  "number": 2,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 1,
                "home": 1
              },
              {
                "periodDescriptor": {
                  "number": 3,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 0,
                "home": 0
              },
              {
                "periodDescriptor": {
                  "number": 4,
                  "periodType": "OT",
                  "maxRegulationPeriods": 3
                },
                "away": 0,
                "home": 1
              }
            ],
            "totals": {
              "away": 2,
              "home": 3
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 36,
              "homeValue": 26
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.412698,
              "homeValue": 0.587302
            },
            {
              "category": "powerPlay",
              "awayValue": "1/4",
              "homeValue": "1/3"
            },
            {
              "category": "pim",
              "awayValue": 6,
              "homeValue": 8
            },
            {
              "category": "hits",
              "awayValue": 34,
              "homeValue": 18
            },
            {
              "category": "blockedShots",
              "awayValue": 16,
              "homeValue": 24
            },
            {
              "category": "giveaways",
              "awayValue": 10,
              "homeValue": 21
            },
            {
              "category": "takeaways",
              "awayValue": 5,
              "homeValue": 5
            }
          ]
        }
        """;

    protected override string PlayByPlayJson => """
        {
          "plays": [
            {
              "eventId": 8,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:00",
              "timeRemaining": "20:00",
              "typeCode": 520,
              "typeDescKey": "period-start",
              "sortOrder": 8
            },
            {
              "eventId": 9,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:00",
              "timeRemaining": "20:00",
              "situationCode": "1551",
              "typeCode": 502,
              "typeDescKey": "faceoff",
              "sortOrder": 9,
              "details": {
                "eventOwnerTeamId": 8,
                "losingPlayerId": 8479318,
                "winningPlayerId": 8477503,
                "xCoord": 0,
                "yCoord": 0,
                "zoneCode": "N"
              }
            },
            {
              "eventId": 10,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:29",
              "timeRemaining": "19:31",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 10,
              "details": {
                "xCoord": 78,
                "yCoord": -19,
                "zoneCode": "O",
                "shotType": "backhand",
                "shootingPlayerId": 8477476,
                "goalieInNetId": 8475883,
                "eventOwnerTeamId": 8,
                "awaySOG": 1,
                "homeSOG": 0
              }
            },
            {
              "eventId": 32,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "09:34",
              "timeRemaining": "10:26",
              "situationCode": "1551",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 104,
              "details": {
                "xCoord": 87,
                "yCoord": -3,
                "zoneCode": "O",
                "shotType": "wrap-around",
                "scoringPlayerId": 8477476,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8477503,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8474038,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 8,
                "goalieInNetId": 8475883,
                "awayScore": 1,
                "homeScore": 0,
                "discreteClip": 6336124435112
              }
            },
            {
              "eventId": 175,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "11:02",
              "timeRemaining": "08:58",
              "situationCode": "1551",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 125,
              "details": {
                "xCoord": -55,
                "yCoord": 28,
                "zoneCode": "D",
                "typeCode": "MIN",
                "descKey": "hooking",
                "duration": 2,
                "committedByPlayerId": 8477494,
                "drawnByPlayerId": 8475166,
                "eventOwnerTeamId": 8
              }
            },
            {
              "eventId": 848,
              "periodDescriptor": {
                "number": 4,
                "periodType": "OT",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "01:01",
              "timeRemaining": "03:59",
              "situationCode": "1331",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 734
            }
          ]
        }
        """;
}
