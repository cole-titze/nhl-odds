using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2013020001: TOR @ MTL
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2013020001
/// </summary>
[TestClass]
public class Season2013Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2013;
    protected override int    ExpectedHomeTeamId      => 8;
    protected override int    ExpectedAwayTeamId      => 10;
    protected override string ExpectedHomeTeamAbbr    => "MTL";
    protected override string ExpectedAwayTeamAbbr    => "TOR";
    protected override int    ExpectedHomeGoals        => 3;
    protected override int    ExpectedAwayGoals        => 4;

    protected override string BoxscoreJson => """
        {
          "id": 2013020001,
          "season": 20132014,
          "startTimeUTC": "2013-10-01T23:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 8,
            "abbrev": "MTL"
          },
          "awayTeam": {
            "id": 10,
            "abbrev": "TOR"
          },
          "tvBroadcasts": [
            {
              "id": 4,
              "market": "N",
              "countryCode": "CA",
              "network": "CBC",
              "sequenceNumber": 101
            },
            {
              "id": 33,
              "market": "N",
              "countryCode": "CA",
              "network": "RDS",
              "sequenceNumber": 145
            }
          ],
          "venue": {
            "default": "Centre Bell"
          },
          "venueLocation": {
            "default": "Montreal",
            "fr": "Montr\u00e9al"
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
                "home": 2
              },
              {
                "periodDescriptor": {
                  "number": 2,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 2,
                "home": 0
              },
              {
                "periodDescriptor": {
                  "number": 3,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 1,
                "home": 1
              }
            ],
            "totals": {
              "away": 4,
              "home": 3
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 38,
              "homeValue": 37
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.527273,
              "homeValue": 0.472727
            },
            {
              "category": "powerPlay",
              "awayValue": "1/7",
              "homeValue": "0/4"
            },
            {
              "category": "pim",
              "awayValue": 47,
              "homeValue": 53
            },
            {
              "category": "hits",
              "awayValue": 25,
              "homeValue": 21
            },
            {
              "category": "blockedShots",
              "awayValue": 17,
              "homeValue": 15
            },
            {
              "category": "giveaways",
              "awayValue": 5,
              "homeValue": 10
            },
            {
              "category": "takeaways",
              "awayValue": 3,
              "homeValue": 7
            }
          ]
        }
        """;

    protected override string PlayByPlayJson => """
        {
          "plays": [
            {
              "eventId": 51,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:00",
              "timeRemaining": "20:00",
              "typeCode": 520,
              "typeDescKey": "period-start",
              "sortOrder": 5
            },
            {
              "eventId": 280,
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
              "sortOrder": 12,
              "details": {
                "eventOwnerTeamId": 10,
                "losingPlayerId": 8469521,
                "winningPlayerId": 8475098,
                "xCoord": 0,
                "yCoord": 0,
                "zoneCode": "N"
              }
            },
            {
              "eventId": 54,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "01:36",
              "timeRemaining": "18:24",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 18,
              "details": {
                "xCoord": -82,
                "yCoord": -22,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8475848,
                "goalieInNetId": 8473503,
                "eventOwnerTeamId": 8,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 31,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "06:23",
              "timeRemaining": "13:37",
              "situationCode": "1551",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 98,
              "details": {
                "xCoord": -61,
                "yCoord": -14,
                "zoneCode": "O",
                "typeCode": "MIN",
                "descKey": "tripping",
                "duration": 2,
                "committedByPlayerId": 8474189,
                "drawnByPlayerId": 8470207,
                "eventOwnerTeamId": 8
              }
            },
            {
              "eventId": 87,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "08:01",
              "timeRemaining": "11:59",
              "situationCode": "1531",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 119,
              "details": {
                "xCoord": 84,
                "yCoord": -8,
                "zoneCode": "O",
                "shotType": "wrist",
                "scoringPlayerId": 8474037,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8473548,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8471742,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 10,
                "goalieInNetId": 8471679,
                "awayScore": 1,
                "homeScore": 0
              }
            },
            {
              "eventId": 557,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 727
            }
          ]
        }
        """;
}
