using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2010020001: MTL @ TOR
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2010020001
/// </summary>
[TestClass]
public class Season2010Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2010;
    protected override int    ExpectedHomeTeamId      => 10;
    protected override int    ExpectedAwayTeamId      => 8;
    protected override string ExpectedHomeTeamAbbr    => "TOR";
    protected override string ExpectedAwayTeamAbbr    => "MTL";
    protected override int    ExpectedHomeGoals        => 3;
    protected override int    ExpectedAwayGoals        => 2;

    protected override string BoxscoreJson => """
        {
          "id": 2010020001,
          "season": 20102011,
          "startTimeUTC": "2010-10-07T23:00:00Z",
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
              "id": 81,
              "market": "N",
              "countryCode": "CA",
              "network": "CBC (HD)",
              "sequenceNumber": 102
            },
            {
              "id": 96,
              "market": "N",
              "countryCode": "CA",
              "network": "RDS (HD)",
              "sequenceNumber": 127
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
                "away": 0,
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
              "away": 2,
              "home": 3
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 28,
              "homeValue": 24
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.534884,
              "homeValue": 0.465116
            },
            {
              "category": "powerPlay",
              "awayValue": "0/3",
              "homeValue": "0/5"
            },
            {
              "category": "pim",
              "awayValue": 10,
              "homeValue": 6
            },
            {
              "category": "hits",
              "awayValue": 34,
              "homeValue": 27
            },
            {
              "category": "blockedShots",
              "awayValue": 21,
              "homeValue": 22
            },
            {
              "category": "giveaways",
              "awayValue": 7,
              "homeValue": 16
            },
            {
              "category": "takeaways",
              "awayValue": 7,
              "homeValue": 6
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
                "losingPlayerId": 8470283,
                "winningPlayerId": 8467351,
                "xCoord": 0,
                "yCoord": 0,
                "zoneCode": "N"
              }
            },
            {
              "eventId": 8,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "01:26",
              "timeRemaining": "18:34",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 16,
              "details": {
                "xCoord": 81,
                "yCoord": -7,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8474189,
                "goalieInNetId": 8462044,
                "eventOwnerTeamId": 8,
                "awaySOG": 1,
                "homeSOG": 0
              }
            },
            {
              "eventId": 22,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "06:42",
              "timeRemaining": "13:18",
              "situationCode": "1551",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 79,
              "details": {
                "xCoord": -69,
                "yCoord": -8,
                "zoneCode": "O",
                "shotType": "tip-in",
                "scoringPlayerId": 8470283,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8470602,
                "assist1PlayerTotal": 1,
                "eventOwnerTeamId": 10,
                "goalieInNetId": 8471679,
                "awayScore": 0,
                "homeScore": 1
              }
            },
            {
              "eventId": 96,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "14:02",
              "timeRemaining": "05:58",
              "situationCode": "1560",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 167,
              "details": {
                "xCoord": -81,
                "yCoord": -1,
                "zoneCode": "D",
                "typeCode": "MIN",
                "descKey": "hooking",
                "duration": 2,
                "committedByPlayerId": 8474189,
                "drawnByPlayerId": 8469464,
                "eventOwnerTeamId": 8
              }
            },
            {
              "eventId": 689,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 668
            }
          ]
        }
        """;
}
