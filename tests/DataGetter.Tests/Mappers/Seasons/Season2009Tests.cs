using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2009020001: WSH @ BOS
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2009020001
/// </summary>
[TestClass]
public class Season2009Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2009;
    protected override int    ExpectedHomeTeamId      => 6;
    protected override int    ExpectedAwayTeamId      => 15;
    protected override string ExpectedHomeTeamAbbr    => "BOS";
    protected override string ExpectedAwayTeamAbbr    => "WSH";
    protected override int    ExpectedHomeGoals        => 1;
    protected override int    ExpectedAwayGoals        => 4;

    protected override string BoxscoreJson => """
        {
          "id": 2009020001,
          "season": 20092010,
          "startTimeUTC": "2009-10-01T23:00:00Z",
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
            "id": 15,
            "abbrev": "WSH"
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
                "away": 1,
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
                "away": 2,
                "home": 1
              }
            ],
            "totals": {
              "away": 4,
              "home": 1
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 34,
              "homeValue": 20
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.612245,
              "homeValue": 0.387755
            },
            {
              "category": "powerPlay",
              "awayValue": "2/4",
              "homeValue": "0/5"
            },
            {
              "category": "pim",
              "awayValue": 15,
              "homeValue": 13
            },
            {
              "category": "hits",
              "awayValue": 24,
              "homeValue": 32
            },
            {
              "category": "blockedShots",
              "awayValue": 13,
              "homeValue": 16
            },
            {
              "category": "giveaways",
              "awayValue": 0,
              "homeValue": 6
            },
            {
              "category": "takeaways",
              "awayValue": 7,
              "homeValue": 2
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
                "eventOwnerTeamId": 15,
                "losingPlayerId": 8462118,
                "winningPlayerId": 8459461,
                "xCoord": 0,
                "yCoord": 0,
                "zoneCode": "N"
              }
            },
            {
              "eventId": 53,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:18",
              "timeRemaining": "19:42",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 8,
              "details": {
                "xCoord": -61,
                "yCoord": -7,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8462118,
                "goalieInNetId": 8460535,
                "eventOwnerTeamId": 6,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 37,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "13:22",
              "timeRemaining": "06:38",
              "situationCode": "1551",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 127,
              "details": {
                "xCoord": 78,
                "yCoord": 32,
                "zoneCode": "O",
                "typeCode": "MIN",
                "descKey": "hooking",
                "duration": 2,
                "committedByPlayerId": 8464994,
                "drawnByPlayerId": 8458590,
                "eventOwnerTeamId": 6
              }
            },
            {
              "eventId": 92,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "17:15",
              "timeRemaining": "02:45",
              "situationCode": "1541",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 172,
              "details": {
                "xCoord": -81,
                "yCoord": -11,
                "zoneCode": "O",
                "shotType": "snap",
                "scoringPlayerId": 8469639,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8473563,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8471214,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 15,
                "goalieInNetId": 8460703,
                "awayScore": 1,
                "homeScore": 0
              }
            }
          ]
        }
        """;
}
