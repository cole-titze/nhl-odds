using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2011020001: PHI @ BOS
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2011020001
/// </summary>
[TestClass]
public class Season2011Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2011;
    protected override int    ExpectedHomeTeamId      => 6;
    protected override int    ExpectedAwayTeamId      => 4;
    protected override string ExpectedHomeTeamAbbr    => "BOS";
    protected override string ExpectedAwayTeamAbbr    => "PHI";
    protected override int    ExpectedHomeGoals        => 1;
    protected override int    ExpectedAwayGoals        => 2;

    protected override string BoxscoreJson => """
        {
          "id": 2011020001,
          "season": 20112012,
          "startTimeUTC": "2011-10-06T23:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 6,
            "abbrev": "BOS"
          },
          "awayTeam": {
            "id": 4,
            "abbrev": "PHI"
          },
          "tvBroadcasts": [
            {
              "id": 60,
              "market": "N",
              "countryCode": "US",
              "network": "VERSUS (HD)",
              "sequenceNumber": 33
            }
          ],
          "venue": {
            "default": "TD Garden"
          },
          "venueLocation": {
            "default": "Boston"
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
                "away": 2,
                "home": 1
              },
              {
                "periodDescriptor": {
                  "number": 2,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 0,
                "home": 0
              },
              {
                "periodDescriptor": {
                  "number": 3,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 0,
                "home": 0
              }
            ],
            "totals": {
              "away": 2,
              "home": 1
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 29,
              "homeValue": 23
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.333333,
              "homeValue": 0.666667
            },
            {
              "category": "powerPlay",
              "awayValue": "1/4",
              "homeValue": "1/5"
            },
            {
              "category": "pim",
              "awayValue": 14,
              "homeValue": 10
            },
            {
              "category": "hits",
              "awayValue": 17,
              "homeValue": 19
            },
            {
              "category": "blockedShots",
              "awayValue": 17,
              "homeValue": 9
            },
            {
              "category": "giveaways",
              "awayValue": 5,
              "homeValue": 11
            },
            {
              "category": "takeaways",
              "awayValue": 2,
              "homeValue": 8
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
              "sortOrder": 6,
              "details": {
                "eventOwnerTeamId": 6,
                "losingPlayerId": 8464975,
                "winningPlayerId": 8471276,
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
              "timeInPeriod": "00:47",
              "timeRemaining": "19:13",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 9,
              "details": {
                "xCoord": 32,
                "yCoord": -14,
                "zoneCode": "O",
                "shotType": "snap",
                "shootingPlayerId": 8469619,
                "goalieInNetId": 8468524,
                "eventOwnerTeamId": 6,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 21,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "09:00",
              "timeRemaining": "11:00",
              "situationCode": "1551",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 88,
              "details": {
                "xCoord": -93,
                "yCoord": 14,
                "zoneCode": "O",
                "typeCode": "MIN",
                "descKey": "holding-the-stick",
                "duration": 2,
                "committedByPlayerId": 8474736,
                "drawnByPlayerId": 8466215,
                "eventOwnerTeamId": 4
              }
            },
            {
              "eventId": 75,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "09:42",
              "timeRemaining": "10:18",
              "situationCode": "1451",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 96,
              "details": {
                "xCoord": 75,
                "yCoord": -7,
                "zoneCode": "O",
                "shotType": "backhand",
                "scoringPlayerId": 8473419,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8475794,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8466215,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 6,
                "goalieInNetId": 8468524,
                "awayScore": 0,
                "homeScore": 1
              }
            },
            {
              "eventId": 599,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 618
            }
          ]
        }
        """;
}
