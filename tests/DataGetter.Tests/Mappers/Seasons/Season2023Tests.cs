using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2023020001: NSH @ TBL
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2023020001
/// </summary>
[TestClass]
public class Season2023Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2023;
    protected override int    ExpectedHomeTeamId      => 14;
    protected override int    ExpectedAwayTeamId      => 18;
    protected override string ExpectedHomeTeamAbbr    => "TBL";
    protected override string ExpectedAwayTeamAbbr    => "NSH";
    protected override int    ExpectedHomeGoals        => 5;
    protected override int    ExpectedAwayGoals        => 3;

    protected override string BoxscoreJson => """
        {
          "id": 2023020001,
          "season": 20232024,
          "startTimeUTC": "2023-10-10T21:30:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 14,
            "abbrev": "TBL"
          },
          "awayTeam": {
            "id": 18,
            "abbrev": "NSH"
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
              "id": 329,
              "market": "N",
              "countryCode": "US",
              "network": "ESPN+",
              "sequenceNumber": 16
            }
          ],
          "venue": {
            "default": "Amalie Arena"
          },
          "venueLocation": {
            "default": "Tampa"
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
                "away": 0,
                "home": 1
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
                "home": 4
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
              "awayValue": 31,
              "homeValue": 34
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.433333,
              "homeValue": 0.566667
            },
            {
              "category": "powerPlay",
              "awayValue": "1/4",
              "homeValue": "2/5"
            },
            {
              "category": "pim",
              "awayValue": 10,
              "homeValue": 8
            },
            {
              "category": "hits",
              "awayValue": 23,
              "homeValue": 22
            },
            {
              "category": "blockedShots",
              "awayValue": 10,
              "homeValue": 17
            },
            {
              "category": "giveaways",
              "awayValue": 7,
              "homeValue": 8
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
              "eventId": 102,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:00",
              "timeRemaining": "20:00",
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 520,
              "typeDescKey": "period-start",
              "sortOrder": 8
            },
            {
              "eventId": 101,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:00",
              "timeRemaining": "20:00",
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 502,
              "typeDescKey": "faceoff",
              "sortOrder": 9,
              "details": {
                "eventOwnerTeamId": 18,
                "losingPlayerId": 8478519,
                "winningPlayerId": 8475158,
                "xCoord": 0,
                "yCoord": 0,
                "zoneCode": "N"
              }
            },
            {
              "eventId": 63,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "01:01",
              "timeRemaining": "18:59",
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 22,
              "details": {
                "xCoord": 58,
                "yCoord": -25,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8478178,
                "goalieInNetId": 8477424,
                "eventOwnerTeamId": 14,
                "awaySOG": 0,
                "homeSOG": 1
              }
            },
            {
              "eventId": 154,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "09:48",
              "timeRemaining": "10:12",
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 151,
              "details": {
                "xCoord": 50,
                "yCoord": -16,
                "zoneCode": "O",
                "shotType": "slap",
                "scoringPlayerId": 8476453,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8475167,
                "assist1PlayerTotal": 1,
                "assist2PlayerId": 8478010,
                "assist2PlayerTotal": 1,
                "eventOwnerTeamId": 14,
                "goalieInNetId": 8477424,
                "awayScore": 0,
                "homeScore": 1,
                "highlightClipSharingUrl": "https://nhl.com/video/nikita-kucherov-with-a-goal-vs-nashville-predators-6338805211112",
                "highlightClip": 6338805211112
              },
              "pptReplayUrl": "https://wsr.nhle.com/sprites/20232024/2023020001/ev154.json"
            },
            {
              "eventId": 297,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "12:05",
              "timeRemaining": "07:55",
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 182,
              "details": {
                "xCoord": -94,
                "yCoord": -13,
                "zoneCode": "D",
                "typeCode": "MIN",
                "descKey": "high-sticking",
                "duration": 2,
                "committedByPlayerId": 8480246,
                "drawnByPlayerId": 8481704,
                "eventOwnerTeamId": 14
              }
            },
            {
              "eventId": 872,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "situationCode": "1551",
              "homeTeamDefendingSide": "left",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 805
            }
          ]
        }
        """;
}
