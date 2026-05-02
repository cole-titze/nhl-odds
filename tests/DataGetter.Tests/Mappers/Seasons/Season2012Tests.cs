using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2012020001: PIT @ PHI
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2012020001
/// </summary>
[TestClass]
public class Season2012Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2012;
    protected override int    ExpectedHomeTeamId      => 4;
    protected override int    ExpectedAwayTeamId      => 5;
    protected override string ExpectedHomeTeamAbbr    => "PHI";
    protected override string ExpectedAwayTeamAbbr    => "PIT";
    protected override int    ExpectedHomeGoals        => 1;
    protected override int    ExpectedAwayGoals        => 3;

    protected override string BoxscoreJson => """
        {
          "id": 2012020001,
          "season": 20122013,
          "startTimeUTC": "2013-01-19T20:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 4,
            "abbrev": "PHI"
          },
          "awayTeam": {
            "id": 5,
            "abbrev": "PIT"
          },
          "tvBroadcasts": [
            {
              "id": 92,
              "market": "N",
              "countryCode": "US",
              "network": "NBC (HD)",
              "sequenceNumber": 24
            },
            {
              "id": 230,
              "market": "N",
              "countryCode": "CA",
              "network": "RDS2",
              "sequenceNumber": 146
            }
          ],
          "venue": {
            "default": "Wells Fargo Center"
          },
          "venueLocation": {
            "default": "Philadelphia",
            "fr": "Philadelphie"
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
                "away": 1,
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
              "awayValue": 27,
              "homeValue": 27
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.564516,
              "homeValue": 0.435484
            },
            {
              "category": "powerPlay",
              "awayValue": "2/3",
              "homeValue": "0/5"
            },
            {
              "category": "pim",
              "awayValue": 10,
              "homeValue": 6
            },
            {
              "category": "hits",
              "awayValue": 37,
              "homeValue": 40
            },
            {
              "category": "blockedShots",
              "awayValue": 12,
              "homeValue": 12
            },
            {
              "category": "giveaways",
              "awayValue": 8,
              "homeValue": 12
            },
            {
              "category": "takeaways",
              "awayValue": 10,
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
                "eventOwnerTeamId": 5,
                "losingPlayerId": 8473512,
                "winningPlayerId": 8471675,
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
              "timeInPeriod": "00:29",
              "timeRemaining": "19:31",
              "situationCode": "1551",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 8,
              "details": {
                "xCoord": 43,
                "yCoord": 24,
                "zoneCode": "O",
                "shotType": "snap",
                "shootingPlayerId": 8468498,
                "goalieInNetId": 8468524,
                "eventOwnerTeamId": 5,
                "awaySOG": 1,
                "homeSOG": 0
              }
            },
            {
              "eventId": 15,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "02:47",
              "timeRemaining": "17:13",
              "situationCode": "1551",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 33,
              "details": {
                "xCoord": 57,
                "yCoord": -31,
                "zoneCode": "D",
                "typeCode": "MIN",
                "descKey": "interference",
                "duration": 2,
                "committedByPlayerId": 8470601,
                "drawnByPlayerId": 8471675,
                "eventOwnerTeamId": 4
              }
            },
            {
              "eventId": 66,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "04:40",
              "timeRemaining": "15:20",
              "situationCode": "1541",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 57,
              "details": {
                "xCoord": 31,
                "yCoord": -15,
                "zoneCode": "O",
                "shotType": "slap",
                "scoringPlayerId": 8468542,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8471702,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8474091,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 5,
                "goalieInNetId": 8468524,
                "awayScore": 1,
                "homeScore": 0
              }
            },
            {
              "eventId": 769,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 692
            }
          ]
        }
        """;
}
