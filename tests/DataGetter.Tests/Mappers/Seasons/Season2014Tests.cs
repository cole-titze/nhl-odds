using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2014020001: MTL @ TOR
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2014020001
/// </summary>
[TestClass]
public class Season2014Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2014;
    protected override int    ExpectedHomeTeamId      => 10;
    protected override int    ExpectedAwayTeamId      => 8;
    protected override string ExpectedHomeTeamAbbr    => "TOR";
    protected override string ExpectedAwayTeamAbbr    => "MTL";
    protected override int    ExpectedHomeGoals        => 3;
    protected override int    ExpectedAwayGoals        => 4;

    protected override string BoxscoreJson => """
        {
          "id": 2014020001,
          "season": 20142015,
          "startTimeUTC": "2014-10-08T23:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
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
              "id": 281,
              "market": "N",
              "countryCode": "CA",
              "network": "TVAS",
              "sequenceNumber": 120
            }
          ],
          "venue": {
            "default": "Air Canada Centre"
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
                "home": 2
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
                "away": 2,
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
              "awayValue": 32,
              "homeValue": 27
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.587302,
              "homeValue": 0.412698
            },
            {
              "category": "powerPlay",
              "awayValue": "0/2",
              "homeValue": "1/3"
            },
            {
              "category": "pim",
              "awayValue": 6,
              "homeValue": 4
            },
            {
              "category": "hits",
              "awayValue": 21,
              "homeValue": 35
            },
            {
              "category": "blockedShots",
              "awayValue": 20,
              "homeValue": 11
            },
            {
              "category": "giveaways",
              "awayValue": 11,
              "homeValue": 13
            },
            {
              "category": "takeaways",
              "awayValue": 9,
              "homeValue": 10
            }
          ]
        }
        """;

    protected override string PlayByPlayJson => """
        {
          "plays": [
            {
              "eventId": 5,
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
              "eventId": 6,
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
              "sortOrder": 6,
              "details": {
                "eventOwnerTeamId": 8,
                "losingPlayerId": 8475098,
                "winningPlayerId": 8471976,
                "xCoord": 0,
                "yCoord": 0,
                "zoneCode": "N"
              }
            },
            {
              "eventId": 9,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:47",
              "timeRemaining": "19:13",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 15,
              "details": {
                "xCoord": -54,
                "yCoord": 29,
                "zoneCode": "O",
                "shotType": "backhand",
                "shootingPlayerId": 8475098,
                "goalieInNetId": 8471679,
                "eventOwnerTeamId": 10,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 25,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "04:42",
              "timeRemaining": "15:18",
              "situationCode": "1551",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 68,
              "details": {
                "xCoord": 82,
                "yCoord": -15,
                "zoneCode": "O",
                "shotType": "wrist",
                "scoringPlayerId": 8474157,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8469707,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8471296,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 8,
                "goalieInNetId": 8473541,
                "awayScore": 1,
                "homeScore": 0
              }
            },
            {
              "eventId": 190,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "12:21",
              "timeRemaining": "07:39",
              "situationCode": "0651",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 158,
              "details": {
                "xCoord": 96,
                "yCoord": 1,
                "zoneCode": "D",
                "typeCode": "MIN",
                "descKey": "tripping",
                "duration": 2,
                "committedByPlayerId": 8475295,
                "drawnByPlayerId": 8469707,
                "eventOwnerTeamId": 10
              }
            },
            {
              "eventId": 857,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 702
            }
          ]
        }
        """;
}
