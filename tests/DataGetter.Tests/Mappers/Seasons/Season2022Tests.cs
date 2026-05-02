using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2022020001: SJS @ NSH
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2022020001
/// </summary>
[TestClass]
public class Season2022Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2022;
    protected override int    ExpectedHomeTeamId      => 18;
    protected override int    ExpectedAwayTeamId      => 28;
    protected override string ExpectedHomeTeamAbbr    => "NSH";
    protected override string ExpectedAwayTeamAbbr    => "SJS";
    protected override int    ExpectedHomeGoals        => 4;
    protected override int    ExpectedAwayGoals        => 1;

    protected override string BoxscoreJson => """
        {
          "id": 2022020001,
          "season": 20222023,
          "startTimeUTC": "2022-10-07T18:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 18,
            "abbrev": "NSH"
          },
          "awayTeam": {
            "id": 28,
            "abbrev": "SJS"
          },
          "tvBroadcasts": [
            {
              "id": 324,
              "market": "N",
              "countryCode": "US",
              "network": "NHLN",
              "sequenceNumber": 35
            },
            {
              "id": 314,
              "market": "A",
              "countryCode": "US",
              "network": "NBCSCA",
              "sequenceNumber": 393
            }
          ],
          "venue": {
            "default": "O2 Czech Republic"
          },
          "venueLocation": {
            "default": "Prague",
            "cs": "Praha",
            "de": "Prag",
            "fi": "Praha",
            "sk": "Praha",
            "sv": "Prag"
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
                "away": 0,
                "home": 2
              },
              {
                "periodDescriptor": {
                  "number": 3,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 0,
                "home": 1
              }
            ],
            "totals": {
              "away": 1,
              "home": 4
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 31,
              "homeValue": 32
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.545455,
              "homeValue": 0.454545
            },
            {
              "category": "powerPlay",
              "awayValue": "0/4",
              "homeValue": "0/4"
            },
            {
              "category": "pim",
              "awayValue": 13,
              "homeValue": 13
            },
            {
              "category": "hits",
              "awayValue": 22,
              "homeValue": 23
            },
            {
              "category": "blockedShots",
              "awayValue": 23,
              "homeValue": 11
            },
            {
              "category": "giveaways",
              "awayValue": 8,
              "homeValue": 3
            },
            {
              "category": "takeaways",
              "awayValue": 3,
              "homeValue": 4
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
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
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
              "homeTeamDefendingSide": "right",
              "typeCode": 502,
              "typeDescKey": "faceoff",
              "sortOrder": 9,
              "details": {
                "eventOwnerTeamId": 18,
                "losingPlayerId": 8476881,
                "winningPlayerId": 8475798,
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
              "timeInPeriod": "00:23",
              "timeRemaining": "19:37",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 12,
              "details": {
                "xCoord": 44,
                "yCoord": 8,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8478414,
                "goalieInNetId": 8477424,
                "eventOwnerTeamId": 28,
                "awaySOG": 1,
                "homeSOG": 0
              }
            },
            {
              "eventId": 14,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "01:01",
              "timeRemaining": "18:59",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 22,
              "details": {
                "xCoord": -74,
                "yCoord": -5,
                "zoneCode": "O",
                "shotType": "wrist",
                "scoringPlayerId": 8480748,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8475218,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8474151,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 18,
                "goalieInNetId": 8473503,
                "awayScore": 0,
                "homeScore": 1,
                "discreteClip": 6335818502112
              }
            },
            {
              "eventId": 167,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "06:30",
              "timeRemaining": "13:30",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 88,
              "details": {
                "xCoord": -22,
                "yCoord": 34,
                "zoneCode": "N",
                "typeCode": "MIN",
                "descKey": "slashing",
                "duration": 2,
                "committedByPlayerId": 8475793,
                "drawnByPlayerId": 8479393,
                "eventOwnerTeamId": 18
              }
            },
            {
              "eventId": 1012,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "homeTeamDefendingSide": "right",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 663
            }
          ]
        }
        """;
}
