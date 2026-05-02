using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2020020001: PIT @ PHI
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2020020001
/// </summary>
[TestClass]
public class Season2020Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2020;
    protected override int    ExpectedHomeTeamId      => 4;
    protected override int    ExpectedAwayTeamId      => 5;
    protected override string ExpectedHomeTeamAbbr    => "PHI";
    protected override string ExpectedAwayTeamAbbr    => "PIT";
    protected override int    ExpectedHomeGoals        => 6;
    protected override int    ExpectedAwayGoals        => 3;

    protected override string BoxscoreJson => """
        {
          "id": 2020020001,
          "season": 20202021,
          "startTimeUTC": "2021-01-13T22:30:00Z",
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
              "id": 241,
              "market": "N",
              "countryCode": "US",
              "network": "NBCSN",
              "sequenceNumber": 25
            },
            {
              "id": 284,
              "market": "N",
              "countryCode": "CA",
              "network": "SN1",
              "sequenceNumber": 113
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
                "home": 1
              },
              {
                "periodDescriptor": {
                  "number": 3,
                  "periodType": "REG",
                  "maxRegulationPeriods": 3
                },
                "away": 1,
                "home": 3
              }
            ],
            "totals": {
              "away": 3,
              "home": 6
            }
          },
          "teamGameStats": [
            {
              "category": "sog",
              "awayValue": 34,
              "homeValue": 27
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.42,
              "homeValue": 0.58
            },
            {
              "category": "powerPlay",
              "awayValue": "1/3",
              "homeValue": "2/3"
            },
            {
              "category": "pim",
              "awayValue": 6,
              "homeValue": 6
            },
            {
              "category": "hits",
              "awayValue": 23,
              "homeValue": 31
            },
            {
              "category": "blockedShots",
              "awayValue": 11,
              "homeValue": 13
            },
            {
              "category": "giveaways",
              "awayValue": 10,
              "homeValue": 10
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
              "eventId": 51,
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
              "eventId": 52,
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
                "eventOwnerTeamId": 5,
                "losingPlayerId": 8476461,
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
              "timeInPeriod": "00:16",
              "timeRemaining": "19:44",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 10,
              "details": {
                "xCoord": -74,
                "yCoord": 29,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8478439,
                "goalieInNetId": 8477465,
                "eventOwnerTeamId": 4,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 70,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "05:12",
              "timeRemaining": "14:48",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 61,
              "details": {
                "xCoord": 81,
                "yCoord": -5,
                "zoneCode": "O",
                "shotType": "snap",
                "scoringPlayerId": 8476873,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8479293,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8477955,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 5,
                "goalieInNetId": 8479394,
                "awayScore": 1,
                "homeScore": 0,
                "discreteClip": 6335943339112
              }
            },
            {
              "eventId": 113,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "05:44",
              "timeRemaining": "14:16",
              "situationCode": "0651",
              "homeTeamDefendingSide": "right",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 67,
              "details": {
                "xCoord": 95,
                "yCoord": 22,
                "zoneCode": "D",
                "typeCode": "MIN",
                "descKey": "holding",
                "duration": 2,
                "committedByPlayerId": 8476872,
                "drawnByPlayerId": 8471675,
                "eventOwnerTeamId": 4
              }
            },
            {
              "eventId": 665,
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
              "sortOrder": 620
            }
          ]
        }
        """;
}
