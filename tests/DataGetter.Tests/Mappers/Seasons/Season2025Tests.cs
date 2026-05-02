using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2025020001: CHI @ FLA
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2025020001
/// </summary>
[TestClass]
public class Season2025Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2025;
    protected override int    ExpectedHomeTeamId      => 13;
    protected override int    ExpectedAwayTeamId      => 16;
    protected override string ExpectedHomeTeamAbbr    => "FLA";
    protected override string ExpectedAwayTeamAbbr    => "CHI";
    protected override int    ExpectedHomeGoals        => 3;
    protected override int    ExpectedAwayGoals        => 2;

    protected override string BoxscoreJson => """
        {
          "id": 2025020001,
          "season": 20252026,
          "startTimeUTC": "2025-10-07T21:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 13,
            "abbrev": "FLA"
          },
          "awayTeam": {
            "id": 16,
            "abbrev": "CHI"
          },
          "tvBroadcasts": [
            {
              "id": 309,
              "market": "N",
              "countryCode": "US",
              "network": "ESPN",
              "sequenceNumber": 10
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
            "default": "Amerant Bank Arena"
          },
          "venueLocation": {
            "default": "Sunrise"
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
                "away": 0,
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
              "awayValue": 19,
              "homeValue": 37
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.414286,
              "homeValue": 0.585714
            },
            {
              "category": "powerPlay",
              "awayValue": "0/3",
              "homeValue": "1/2"
            },
            {
              "category": "pim",
              "awayValue": 9,
              "homeValue": 11
            },
            {
              "category": "hits",
              "awayValue": 27,
              "homeValue": 28
            },
            {
              "category": "blockedShots",
              "awayValue": 16,
              "homeValue": 15
            },
            {
              "category": "giveaways",
              "awayValue": 19,
              "homeValue": 17
            },
            {
              "category": "takeaways",
              "awayValue": 4,
              "homeValue": 3
            }
          ]
        }
        """;

    protected override string PlayByPlayJson => """
        {
          "plays": [
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
              "typeCode": 520,
              "typeDescKey": "period-start",
              "sortOrder": 8
            },
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
              "typeCode": 502,
              "typeDescKey": "faceoff",
              "sortOrder": 11,
              "details": {
                "eventOwnerTeamId": 16,
                "losingPlayerId": 8477935,
                "winningPlayerId": 8477450,
                "xCoord": 0,
                "yCoord": 0,
                "zoneCode": "N"
              }
            },
            {
              "eventId": 71,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "01:16",
              "timeRemaining": "18:44",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 29,
              "details": {
                "xCoord": -58,
                "yCoord": -22,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8478542,
                "goalieInNetId": 8481519,
                "eventOwnerTeamId": 13,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 218,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "06:58",
              "timeRemaining": "13:02",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 125,
              "details": {
                "xCoord": 2,
                "yCoord": 2,
                "zoneCode": "N",
                "typeCode": "MIN",
                "descKey": "slashing",
                "duration": 2,
                "committedByPlayerId": 8484783,
                "drawnByPlayerId": 8482113,
                "eventOwnerTeamId": 16
              }
            },
            {
              "eventId": 258,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "10:03",
              "timeRemaining": "09:57",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 166,
              "details": {
                "xCoord": 66,
                "yCoord": -1,
                "zoneCode": "O",
                "shotType": "snap",
                "scoringPlayerId": 8483493,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8477479,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8476882,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 16,
                "goalieInNetId": 8475683,
                "awayScore": 1,
                "homeScore": 0,
                "highlightClipSharingUrl": "https://nhl.com/video/chi-fla-nazar-scores-goal-against-sergei-bobrovsky-6382220522112",
                "highlightClipSharingUrlFr": "https://nhl.com/fr/video/chi-fla-nazar-marque-un-but-contre-sergei-bobrovsky-6382220133112",
                "highlightClip": 6382220522112,
                "highlightClipFr": 6382220133112,
                "discreteClip": 6382219551112,
                "discreteClipFr": 6382220713112
              },
              "pptReplayUrl": "https://wsr.nhle.com/sprites/20252026/2025020001/ev258.json"
            },
            {
              "eventId": 584,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "situationCode": "0651",
              "homeTeamDefendingSide": "right",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 872
            }
          ]
        }
        """;
}
