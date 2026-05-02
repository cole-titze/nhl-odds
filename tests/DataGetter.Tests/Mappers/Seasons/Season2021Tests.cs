using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2021020001: PIT @ TBL
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2021020001
/// </summary>
[TestClass]
public class Season2021Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2021;
    protected override int    ExpectedHomeTeamId      => 14;
    protected override int    ExpectedAwayTeamId      => 5;
    protected override string ExpectedHomeTeamAbbr    => "TBL";
    protected override string ExpectedAwayTeamAbbr    => "PIT";
    protected override int    ExpectedHomeGoals        => 2;
    protected override int    ExpectedAwayGoals        => 6;

    protected override string BoxscoreJson => """
        {
          "id": 2021020001,
          "season": 20212022,
          "startTimeUTC": "2021-10-12T23:30:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 14,
            "abbrev": "TBL"
          },
          "awayTeam": {
            "id": 5,
            "abbrev": "PIT"
          },
          "tvBroadcasts": [
            {
              "id": 309,
              "market": "N",
              "countryCode": "US",
              "network": "ESPN",
              "sequenceNumber": 10
            },
            {
              "id": 329,
              "market": "N",
              "countryCode": "US",
              "network": "ESPN+",
              "sequenceNumber": 16
            }
          ],
          "venue": {
            "default": "Amalie Arena"
          },
          "venueLocation": {
            "default": "Tampa"
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
                "away": 0,
                "home": 0
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
                "away": 4,
                "home": 2
              }
            ],
            "totals": {
              "away": 6,
              "home": 2
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 35,
              "homeValue": 28
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.492754,
              "homeValue": 0.507246
            },
            {
              "category": "powerPlay",
              "awayValue": "0/1",
              "homeValue": "0/1"
            },
            {
              "category": "pim",
              "awayValue": 2,
              "homeValue": 2
            },
            {
              "category": "hits",
              "awayValue": 28,
              "homeValue": 31
            },
            {
              "category": "blockedShots",
              "awayValue": 15,
              "homeValue": 11
            },
            {
              "category": "giveaways",
              "awayValue": 1,
              "homeValue": 6
            },
            {
              "category": "takeaways",
              "awayValue": 2,
              "homeValue": 6
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
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 520,
              "typeDescKey": "period-start",
              "sortOrder": 8
            },
            {
              "eventId": 52,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:00",
              "timeRemaining": "20:00",
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 502,
              "typeDescKey": "faceoff",
              "sortOrder": 9,
              "details": {
                "eventOwnerTeamId": 5,
                "losingPlayerId": 8478010,
                "winningPlayerId": 8470604,
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
              "timeInPeriod": "01:03",
              "timeRemaining": "18:57",
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 18,
              "details": {
                "xCoord": 61,
                "yCoord": -32,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8474564,
                "goalieInNetId": 8477465,
                "eventOwnerTeamId": 14,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 260,
              "periodDescriptor": {
                "number": 2,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:12",
              "timeRemaining": "19:48",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 227,
              "details": {
                "xCoord": 74,
                "yCoord": -9,
                "zoneCode": "O",
                "shotType": "snap",
                "scoringPlayerId": 8478046,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8470604,
                "assist1PlayerTotal": 1,
                "eventOwnerTeamId": 5,
                "goalieInNetId": 8476883,
                "awayScore": 1,
                "homeScore": 0,
                "discreteClip": 6335892741112
              }
            },
            {
              "eventId": 250,
              "periodDescriptor": {
                "number": 2,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "12:56",
              "timeRemaining": "07:04",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 353,
              "details": {
                "xCoord": -43,
                "yCoord": 38,
                "zoneCode": "D",
                "typeCode": "MIN",
                "descKey": "tripping",
                "duration": 2,
                "committedByPlayerId": 8478046,
                "drawnByPlayerId": 8473986,
                "eventOwnerTeamId": 5
              }
            },
            {
              "eventId": 613,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "homeTeamDefendingSide": "left",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 634
            }
          ]
        }
        """;
}
