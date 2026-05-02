using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2019020001: OTT @ TOR
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2019020001
/// </summary>
[TestClass]
public class Season2019Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2019;
    protected override int    ExpectedHomeTeamId      => 10;
    protected override int    ExpectedAwayTeamId      => 9;
    protected override string ExpectedHomeTeamAbbr    => "TOR";
    protected override string ExpectedAwayTeamAbbr    => "OTT";
    protected override int    ExpectedHomeGoals        => 5;
    protected override int    ExpectedAwayGoals        => 3;

    protected override string BoxscoreJson => """
        {
          "id": 2019020001,
          "season": 20192020,
          "startTimeUTC": "2019-10-02T23:00:00Z",
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
            "id": 9,
            "abbrev": "OTT"
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
              "id": 283,
              "market": "N",
              "countryCode": "CA",
              "network": "SN360",
              "sequenceNumber": 114
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
                "home": 0
              },
              {
                "periodDescriptor": {
                  "number": 2,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 1,
                "home": 4
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
              "away": 3,
              "home": 5
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 26,
              "homeValue": 42
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.575342,
              "homeValue": 0.424658
            },
            {
              "category": "powerPlay",
              "awayValue": "0/3",
              "homeValue": "1/5"
            },
            {
              "category": "pim",
              "awayValue": 10,
              "homeValue": 6
            },
            {
              "category": "hits",
              "awayValue": 44,
              "homeValue": 17
            },
            {
              "category": "blockedShots",
              "awayValue": 17,
              "homeValue": 9
            },
            {
              "category": "giveaways",
              "awayValue": 8,
              "homeValue": 12
            },
            {
              "category": "takeaways",
              "awayValue": 7,
              "homeValue": 13
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
              "sortOrder": 10,
              "details": {
                "eventOwnerTeamId": 10,
                "losingPlayerId": 8478400,
                "winningPlayerId": 8475166,
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
              "timeInPeriod": "00:25",
              "timeRemaining": "19:35",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 11,
              "details": {
                "xCoord": 85,
                "yCoord": -1,
                "zoneCode": "O",
                "shotType": "tip-in",
                "scoringPlayerId": 8480801,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8477015,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8478400,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 9,
                "goalieInNetId": 8475883,
                "awayScore": 1,
                "homeScore": 0,
                "discreteClip": 6336030580112
              }
            },
            {
              "eventId": 153,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:56",
              "timeRemaining": "19:04",
              "situationCode": "1560",
              "homeTeamDefendingSide": "right",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 22,
              "details": {
                "xCoord": -9,
                "yCoord": -29,
                "zoneCode": "N",
                "typeCode": "MIN",
                "descKey": "tripping",
                "duration": 2,
                "committedByPlayerId": 8474589,
                "drawnByPlayerId": 8479675,
                "eventOwnerTeamId": 9
              }
            },
            {
              "eventId": 15,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "01:31",
              "timeRemaining": "18:29",
              "situationCode": "1451",
              "homeTeamDefendingSide": "right",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 28,
              "details": {
                "xCoord": -32,
                "yCoord": -2,
                "zoneCode": "O",
                "shotType": "snap",
                "shootingPlayerId": 8476853,
                "goalieInNetId": 8467950,
                "eventOwnerTeamId": 10,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 844,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 745
            }
          ]
        }
        """;
}
