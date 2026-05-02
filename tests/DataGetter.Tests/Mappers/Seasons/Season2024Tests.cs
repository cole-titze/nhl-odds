using DataGetter.Tests.Mappers.Seasons;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Game 2024020001: NJD @ BUF
/// Real API fixtures captured from api-web.nhle.com/v1/gamecenter/2024020001
/// </summary>
[TestClass]
public class Season2024Tests : SeasonParsingTestBase
{
    protected override int    ExpectedSeasonStartYear => 2024;
    protected override int    ExpectedHomeTeamId      => 7;
    protected override int    ExpectedAwayTeamId      => 1;
    protected override string ExpectedHomeTeamAbbr    => "BUF";
    protected override string ExpectedAwayTeamAbbr    => "NJD";
    protected override int    ExpectedHomeGoals        => 1;
    protected override int    ExpectedAwayGoals        => 4;

    protected override string BoxscoreJson => """
        {
          "id": 2024020001,
          "season": 20242025,
          "startTimeUTC": "2024-10-04T17:00:00Z",
          "gameState": "OFF",
          "periodDescriptor": {
            "number": 3,
            "periodType": "REG",
            "maxRegulationPeriods": 3
          },
          "homeTeam": {
            "id": 7,
            "abbrev": "BUF"
          },
          "awayTeam": {
            "id": 1,
            "abbrev": "NJD"
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
              "id": 282,
              "market": "N",
              "countryCode": "CA",
              "network": "SN",
              "sequenceNumber": 107
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
                "away": 2,
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
                "away": 1,
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
              "awayValue": 23,
              "homeValue": 31
            },
            {
              "category": "faceoffWinningPctg",
              "awayValue": 0.423729,
              "homeValue": 0.576271
            },
            {
              "category": "powerPlay",
              "awayValue": "0/2",
              "homeValue": "0/4"
            },
            {
              "category": "pim",
              "awayValue": 8,
              "homeValue": 4
            },
            {
              "category": "hits",
              "awayValue": 34,
              "homeValue": 29
            },
            {
              "category": "blockedShots",
              "awayValue": 18,
              "homeValue": 15
            },
            {
              "category": "giveaways",
              "awayValue": 22,
              "homeValue": 12
            },
            {
              "category": "takeaways",
              "awayValue": 2,
              "homeValue": 5
            }
          ]
        }
        """;

    protected override string PlayByPlayJson => """
        {
          "plays": [
            {
              "eventId": 152,
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
              "sortOrder": 10
            },
            {
              "eventId": 151,
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
                "eventOwnerTeamId": 1,
                "losingPlayerId": 8478043,
                "winningPlayerId": 8480002,
                "xCoord": 0,
                "yCoord": 0,
                "zoneCode": "N"
              }
            },
            {
              "eventId": 103,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "00:08",
              "timeRemaining": "19:52",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 506,
              "typeDescKey": "shot-on-goal",
              "sortOrder": 13,
              "details": {
                "xCoord": 56,
                "yCoord": -39,
                "zoneCode": "O",
                "shotType": "wrist",
                "shootingPlayerId": 8483495,
                "goalieInNetId": 8480045,
                "eventOwnerTeamId": 1,
                "awaySOG": 1,
                "homeSOG": 0
              }
            },
            {
              "eventId": 135,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "01:37",
              "timeRemaining": "18:23",
              "situationCode": "1560",
              "homeTeamDefendingSide": "right",
              "typeCode": 509,
              "typeDescKey": "penalty",
              "sortOrder": 45,
              "details": {
                "xCoord": 1,
                "yCoord": -37,
                "zoneCode": "N",
                "typeCode": "MIN",
                "descKey": "slashing",
                "duration": 2,
                "committedByPlayerId": 8475287,
                "drawnByPlayerId": 8479420,
                "eventOwnerTeamId": 1
              }
            },
            {
              "eventId": 274,
              "periodDescriptor": {
                "number": 1,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "08:39",
              "timeRemaining": "11:21",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 505,
              "typeDescKey": "goal",
              "sortOrder": 146,
              "details": {
                "xCoord": 71,
                "yCoord": -12,
                "zoneCode": "O",
                "shotType": "snap",
                "scoringPlayerId": 8476474,
                "scoringPlayerTotal": 1,
                "assist1PlayerId": 8480192,
                "assist1PlayerTotal": 1,
                "eventOwnerTeamId": 1,
                "goalieInNetId": 8480045,
                "awayScore": 1,
                "homeScore": 0,
                "highlightClipSharingUrl": "https://nhl.com/video/njd-buf-noesen-scores-goal-against-ukko-pekka-luukkonen-6362848229112",
                "highlightClip": 6362848229112,
                "discreteClip": 6362846260112
              },
              "pptReplayUrl": "https://wsr.nhle.com/sprites/20242025/2024020001/ev274.json"
            },
            {
              "eventId": 1730820,
              "periodDescriptor": {
                "number": 3,
                "periodType": "REG",
                "maxRegulationPeriods": 3
              },
              "timeInPeriod": "20:00",
              "timeRemaining": "00:00",
              "situationCode": "1551",
              "homeTeamDefendingSide": "right",
              "typeCode": 524,
              "typeDescKey": "game-end",
              "sortOrder": 874
            }
          ]
        }
        """;
}
