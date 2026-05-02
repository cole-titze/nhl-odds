using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2016020001: TOR @ OTT
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2016020001
/// </summary>
[TestClass]
public class Season2016Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2016;
    protected override int    ExpectedHomeTeamId      => 9;
    protected override int    ExpectedAwayTeamId      => 10;
    protected override string ExpectedHomeTeamAbbr    => "OTT";
    protected override string ExpectedAwayTeamAbbr    => "TOR";
    protected override int    ExpectedHomeGoals        => 5;
    protected override int    ExpectedAwayGoals        => 4;

    protected override string BoxscoreJson => """
        {
          "id": 2016020001,
          "season": 20162017,
          "startTimeUTC": "2016-10-12T23:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 4,
            "periodType": "OT",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 9,
            "abbrev": "OTT"
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
            "default": "Canadian Tire Centre"
          },
          "venueLocation": {
            "default": "Ottawa"
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
                "home": 2
              },
              {
                "periodDescriptor": {
                  "number": 2,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 2,
                "home": 1
              },
              {
                "periodDescriptor": {
                  "number": 3,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 0,
                "home": 1
              },
              {
                "periodDescriptor": {
                  "number": 4,
                  "periodType": "OT",
                  "maxRegulationPeriods": 3
                },
                "away": 0,
                "home": 1
              }
            ],
            "totals": {
              "away": 4,
              "home": 5
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 38,
              "homeValue": 30
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.515152,
              "homeValue": 0.484848
            },
            {
              "category": "powerPlay",
              "awayValue": "0/4",
              "homeValue": "0/2"
            },
            {
              "category": "pim",
              "awayValue": 11,
              "homeValue": 15
            },
            {
              "category": "hits",
              "awayValue": 21,
              "homeValue": 30
            },
            {
              "category": "blockedShots",
              "awayValue": 9,
              "homeValue": 25
            },
            {
              "category": "giveaways",
              "awayValue": 6,
              "homeValue": 11
            },
            {
              "category": "takeaways",
              "awayValue": 8,
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
                "eventOwnerTeamId": 10,
                "losingPlayerId": 8473544,
                "winningPlayerId": 8475172,
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
              "timeInPeriod": "01:11",
              "timeRemaining": "18:49",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 16,
              "details": {
                "xCoord": -77,
                "yCoord": 5,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8478483,
                "goalieInNetId": 8467950,
                "eventOwnerTeamId": 10,
                "awaySOG": 1,
                "homeSOG": 0
              }
            },
            {
              "eventId": 27,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "08:21",
              "timeRemaining": "11:39",
              "situationCode": "1551",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 98,
              "details": {
                "xCoord": -70,
                "yCoord": 1,
                "zoneCode": "O",
                "shotType": "wrist",
                "scoringPlayerId": 8479318,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8475786,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8477939,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 10,
                "goalieInNetId": 8467950,
                "awayScore": 1,
                "homeScore": 0
              }
            },
            {
              "eventId": 47,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "13:25",
              "timeRemaining": "06:35",
              "situationCode": "1551",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 161,
              "details": {
                "xCoord": -90,
                "yCoord": -12,
                "zoneCode": "D",
                "typeCode": "MAJ",
                "descKey": "fighting",
                "duration": 5,
                "committedByPlayerId": 8474697,
                "drawnByPlayerId": 8474709,
                "eventOwnerTeamId": 9
              }
            },
            {
              "eventId": 881,
              "periodDescriptor": {
                "number": 4,
                "periodType": "OT",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:37",
              "timeRemaining": "04:23",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 753
            }
          ]
        }
        """;
}
