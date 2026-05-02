using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2017020001: TOR @ WPG
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2017020001
/// </summary>
[TestClass]
public class Season2017Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2017;
    protected override int    ExpectedHomeTeamId      => 52;
    protected override int    ExpectedAwayTeamId      => 10;
    protected override string ExpectedHomeTeamAbbr    => "WPG";
    protected override string ExpectedAwayTeamAbbr    => "TOR";
    protected override int    ExpectedHomeGoals        => 2;
    protected override int    ExpectedAwayGoals        => 7;

    protected override string BoxscoreJson => """
        {
          "id": 2017020001,
          "season": 20172018,
          "startTimeUTC": "2017-10-04T23:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 52,
            "abbrev": "WPG"
          },
          "awayTeam": {
            "id": 10,
            "abbrev": "TOR"
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
              "id": 281,
              "market": "N",
              "countryCode": "CA",
              "network": "TVAS",
              "sequenceNumber": 120
            }
          ],
          "venue": {
            "default": "Bell MTS Place"
          },
          "venueLocation": {
            "default": "Winnipeg"
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
                "away": 3,
                "home": 0
              },
              {
                "periodDescriptor": {
                  "number": 2,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 1,
                "home": 0
              },
              {
                "periodDescriptor": {
                  "number": 3,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 3,
                "home": 2
              }
            ],
            "totals": {
              "away": 7,
              "home": 2
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 31,
              "homeValue": 37
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.555556,
              "homeValue": 0.444444
            },
            {
              "category": "powerPlay",
              "awayValue": "2/4",
              "homeValue": "0/8"
            },
            {
              "category": "pim",
              "awayValue": 16,
              "homeValue": 8
            },
            {
              "category": "hits",
              "awayValue": 16,
              "homeValue": 18
            },
            {
              "category": "blockedShots",
              "awayValue": 24,
              "homeValue": 14
            },
            {
              "category": "giveaways",
              "awayValue": 10,
              "homeValue": 7
            },
            {
              "category": "takeaways",
              "awayValue": 1,
              "homeValue": 5
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
              "typeCode": 502,
              "typeDescKey": "faceoff",
              "sortOrder": 9,
              "details": {
                "eventOwnerTeamId": 10,
                "losingPlayerId": 8476460,
                "winningPlayerId": 8475172,
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
              "timeInPeriod": "00:38",
              "timeRemaining": "19:22",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 13,
              "details": {
                "xCoord": -36,
                "yCoord": -28,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8477504,
                "goalieInNetId": 8475883,
                "eventOwnerTeamId": 52,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 13,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "02:41",
              "timeRemaining": "17:19",
              "situationCode": "1551",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 43,
              "details": {
                "xCoord": -5,
                "yCoord": 33,
                "zoneCode": "N",
                "typeCode": "MIN",
                "descKey": "high-sticking",
                "duration": 2,
                "committedByPlayerId": 8476853,
                "drawnByPlayerId": 8479293,
                "eventOwnerTeamId": 10
              }
            },
            {
              "eventId": 212,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "15:45",
              "timeRemaining": "04:15",
              "situationCode": "1541",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 191,
              "details": {
                "xCoord": 84,
                "yCoord": -6,
                "zoneCode": "O",
                "shotType": "wrist",
                "scoringPlayerId": 8475172,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8474037,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8475098,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 10,
                "goalieInNetId": 8473461,
                "awayScore": 1,
                "homeScore": 0
              }
            },
            {
              "eventId": 548,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "situationCode": "1551",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 671
            }
          ]
        }
        """;
}
