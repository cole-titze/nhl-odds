using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2015020001: MTL @ TOR
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2015020001
/// </summary>
[TestClass]
public class Season2015Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2015;
    protected override int    ExpectedHomeTeamId      => 10;
    protected override int    ExpectedAwayTeamId      => 8;
    protected override string ExpectedHomeTeamAbbr    => "TOR";
    protected override string ExpectedAwayTeamAbbr    => "MTL";
    protected override int    ExpectedHomeGoals        => 1;
    protected override int    ExpectedAwayGoals        => 3;

    protected override string BoxscoreJson => """
        {
          "id": 2015020001,
          "season": 20152016,
          "startTimeUTC": "2015-10-07T23:00:00Z",
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
                "home": 0
              },
              {
                "periodDescriptor": {
                  "number": 2,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 0,
                "home": 1
              },
              {
                "periodDescriptor": {
                  "number": 3,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 2,
                "home": 0
              }
            ],
            "totals": {
              "away": 3,
              "home": 1
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 29,
              "homeValue": 37
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.46875,
              "homeValue": 0.53125
            },
            {
              "category": "powerPlay",
              "awayValue": "0/1",
              "homeValue": "1/3"
            },
            {
              "category": "pim",
              "awayValue": 8,
              "homeValue": 4
            },
            {
              "category": "hits",
              "awayValue": 26,
              "homeValue": 26
            },
            {
              "category": "blockedShots",
              "awayValue": 15,
              "homeValue": 9
            },
            {
              "category": "giveaways",
              "awayValue": 9,
              "homeValue": 4
            },
            {
              "category": "takeaways",
              "awayValue": 6,
              "homeValue": 7
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
                "winningPlayerId": 8469521,
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
              "timeInPeriod": "00:51",
              "timeRemaining": "19:09",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 17,
              "details": {
                "xCoord": -55,
                "yCoord": 6,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8468504,
                "goalieInNetId": 8471679,
                "eventOwnerTeamId": 10,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 17,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "03:09",
              "timeRemaining": "16:51",
              "situationCode": "1551",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 47,
              "details": {
                "xCoord": 63,
                "yCoord": -27,
                "zoneCode": "O",
                "shotType": "wrist",
                "scoringPlayerId": 8474157,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8474056,
                "assist1PlayerTotal": 1,
                "eventOwnerTeamId": 8,
                "goalieInNetId": 8473541,
                "awayScore": 1,
                "homeScore": 0
              }
            },
            {
              "eventId": 66,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "05:11",
              "timeRemaining": "14:49",
              "situationCode": "1551",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 74,
              "details": {
                "xCoord": -78,
                "yCoord": -35,
                "zoneCode": "O",
                "typeCode": "MIN",
                "descKey": "boarding",
                "duration": 2,
                "committedByPlayerId": 8473463,
                "drawnByPlayerId": 8474056,
                "eventOwnerTeamId": 10
              }
            },
            {
              "eventId": 765,
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
