using System.Text.Json.Nodes;
using Entities.Models.GamePlayEvents;
using Entities.Models.Teams;
using Entities.ServiceModels.Mappers;
using Entities.Types;
using Entities.Types.Enums;
using FluentAssertions;

namespace DataGetter.Tests.Mappers;

/// <summary>
/// Parses real NHL API JSON responses (game 2023020001: TBL 5, NSH 3, 2023-10-10) to verify
/// the JSON field names and shapes the NHL API returns match what the mappers expect.
/// </summary>
[TestClass]
public class NhlApiJsonParsingTests
{
    // Minimal boxscore JSON from api-web.nhle.com/v1/gamecenter/2023020001/boxscore
    private const string BoxscoreJson = """
        {
          "id": 2023020001,
          "season": 20232024,
          "startTimeUTC": "2023-10-10T21:30:00Z",
          "gameState": "OFF",
          "periodDescriptor": { "number": 3, "periodType": "REG", "maxRegulationPeriods": 3 },
          "homeTeam": { "id": 14, "abbrev": "TBL" },
          "awayTeam": { "id": 18, "abbrev": "NSH" },
          "tvBroadcasts": [
            { "id": 309, "market": "N", "countryCode": "US", "network": "ESPN",  "sequenceNumber": 10  },
            { "id": 329, "market": "N", "countryCode": "US", "network": "ESPN+", "sequenceNumber": 16  },
            { "id": 282, "market": "N", "countryCode": "CA", "network": "SN",    "sequenceNumber": 107 },
            { "id": 281, "market": "N", "countryCode": "CA", "network": "TVAS",  "sequenceNumber": 120 }
          ],
          "venue": { "default": "Amalie Arena" },
          "venueLocation": { "default": "Tampa" }
        }
        """;

    // Minimal right-rail JSON from api-web.nhle.com/v1/gamecenter/2023020001/right-rail
    private const string RightRailJson = """
        {
          "linescore": { "totals": { "away": 3, "home": 5 } },
          "teamGameStats": [
            { "category": "sog",                  "awayValue": 31,       "homeValue": 34       },
            { "category": "faceoffWinningPctg",   "awayValue": 0.433333, "homeValue": 0.566667 },
            { "category": "powerPlay",            "awayValue": "1/4",    "homeValue": "2/5"    },
            { "category": "pim",                  "awayValue": 10,       "homeValue": 8        },
            { "category": "hits",                 "awayValue": 23,       "homeValue": 22       },
            { "category": "blockedShots",         "awayValue": 10,       "homeValue": 17       },
            { "category": "giveaways",            "awayValue": 7,        "homeValue": 8        },
            { "category": "takeaways",            "awayValue": 10,       "homeValue": 8        }
          ]
        }
        """;

    // Minimal play-by-play with one of each key event type, using real data from game 2023020001.
    // situationCode is intentionally a JSON string ("1551") to match real API output and exercise GetCoercedInt().
    private const string PlayByPlayJson = """
        {
          "plays": [
            {
              "eventId": 102, "typeCode": 520, "typeDescKey": "period-start", "sortOrder": 8,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "00:00", "timeRemaining": "20:00",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 }
            },
            {
              "eventId": 101, "typeCode": 502, "typeDescKey": "faceoff", "sortOrder": 9,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "00:00", "timeRemaining": "20:00",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "eventOwnerTeamId": 18, "winningPlayerId": 8475158, "losingPlayerId": 8478519, "xCoord": 0, "yCoord": 0, "zoneCode": "N" }
            },
            {
              "eventId": 63, "typeCode": 506, "typeDescKey": "shot-on-goal", "sortOrder": 22,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "01:01", "timeRemaining": "18:59",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "eventOwnerTeamId": 14, "shootingPlayerId": 8478178, "goalieInNetId": 8477424, "shotType": "wrist", "zoneCode": "O", "xCoord": 58, "yCoord": -25 }
            },
            {
              "eventId": 154, "typeCode": 505, "typeDescKey": "goal", "sortOrder": 151,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "09:48", "timeRemaining": "10:12",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "pptReplayUrl": "https://wsr.nhle.com/sprites/20232024/2023020001/ev154.json",
              "details": {
                "eventOwnerTeamId": 14, "scoringPlayerId": 8476453, "goalieInNetId": 8477424,
                "assist1PlayerId": 8475167, "assist2PlayerId": 8478010,
                "shotType": "slap", "zoneCode": "O", "xCoord": 50, "yCoord": -16,
                "highlightClipSharingUrl": "https://nhl.com/video/nikita-kucherov-with-a-goal-vs-nashville-predators-6338805211112",
                "highlightClip": 6338805211112
              }
            },
            {
              "eventId": 297, "typeCode": 509, "typeDescKey": "penalty", "sortOrder": 182,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "12:05", "timeRemaining": "07:55",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": {
                "eventOwnerTeamId": 14, "committedByPlayerId": 8480246, "drawnByPlayerId": 8481704,
                "typeCode": "MIN", "descKey": "high-sticking", "duration": 2,
                "zoneCode": "D", "xCoord": -94, "yCoord": -13
              }
            },
            {
              "eventId": 872, "typeCode": 524, "typeDescKey": "game-end", "sortOrder": 805,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "20:00", "timeRemaining": "00:00",
              "periodDescriptor": { "number": 3, "periodType": "REG", "maxRegulationPeriods": 3 }
            }
          ]
        }
        """;

    [TestMethod]
    public void MapGameResponse_ParsesGameSummaryFields()
    {
        var game = MapGameResponseToGame.Map(
            JsonNode.Parse(BoxscoreJson),
            JsonNode.Parse(RightRailJson),
            JsonNode.Parse(PlayByPlayJson));

        game.Id.Should().Be(2023020001);
        game.HomeTeamId.Should().Be(14);
        game.AwayTeamId.Should().Be(18);
        game.HomeTeamAbbr.Should().Be("TBL");
        game.AwayTeamAbbr.Should().Be("NSH");
        game.SeasonStartYear.Should().Be(2023);
        game.GameDateUTC.ToUniversalTime().Should().Be(new DateTime(2023, 10, 10, 21, 30, 0, DateTimeKind.Utc));
        game.GameType.Should().Be(GameType.Regular);
        game.HasBeenPlayed.Should().BeTrue();
        game.EndPeriod.Should().Be(PeriodType.Regulation);
    }

    [TestMethod]
    public void MapGameResponse_ParsesAllStatFields()
    {
        var game = MapGameResponseToGame.Map(
            JsonNode.Parse(BoxscoreJson),
            JsonNode.Parse(RightRailJson),
            JsonNode.Parse(PlayByPlayJson));

        game.HomeGoals.Should().Be(5);
        game.AwayGoals.Should().Be(3);
        game.Winner.Should().Be(Winner.HOME);
        game.HomeSOG.Should().Be(34);
        game.AwaySOG.Should().Be(31);
        game.HomePPG.Should().Be(2);
        game.AwayPPG.Should().Be(1);
        game.HomePIM.Should().Be(8);
        game.AwayPIM.Should().Be(10);
        game.HomeHits.Should().Be(22);
        game.AwayHits.Should().Be(23);
        game.HomeBlockedShots.Should().Be(17);
        game.AwayBlockedShots.Should().Be(10);
        game.HomeGiveaways.Should().Be(8);
        game.AwayGiveaways.Should().Be(7);
        game.HomeTakeaways.Should().Be(8);
        game.AwayTakeaways.Should().Be(10);
        game.HomeFaceOffWinPercent.Should().BeApproximately(0.566667, 1e-6);
        game.AwayFaceOffWinPercent.Should().BeApproximately(0.433333, 1e-6);
    }

    [TestMethod]
    public void MapGameResponse_ParsesExtendedInfo()
    {
        var game = MapGameResponseToGame.Map(
            JsonNode.Parse(BoxscoreJson),
            JsonNode.Parse(RightRailJson),
            JsonNode.Parse(PlayByPlayJson));

        game.ExtendedInfo.Should().NotBeNull();
        game.ExtendedInfo!.VenueName.Should().Be("Amalie Arena");
        game.ExtendedInfo.VenueLocation.Should().Be("Tampa");
        game.ExtendedInfo.TvBroadcasters.Should().HaveCount(4);

        var espn = game.ExtendedInfo.TvBroadcasters.Single(b => b.NetworkName == "ESPN");
        espn.Id.Should().Be(309);
        espn.MarketAbbreviation.Should().Be("N");
        espn.CountryCode.Should().Be("US");
        espn.SequenceNumber.Should().Be(10);
    }

    [TestMethod]
    public void MapGameResponse_PopulatesGameEvents()
    {
        var game = MapGameResponseToGame.Map(
            JsonNode.Parse(BoxscoreJson),
            JsonNode.Parse(RightRailJson),
            JsonNode.Parse(PlayByPlayJson));

        game.GameEvents.Should().NotBeNull();
        game.GameEvents!.Events.Should().HaveCount(6);
    }

    [TestMethod]
    public void MapGameResponse_FutureGame_HasBeenPlayedIsFalse()
    {
        const string json = """
            {
              "id": 2024020001,
              "season": 20242025,
              "startTimeUTC": "2024-10-08T23:00:00Z",
              "gameState": "FUT",
              "homeTeam": { "id": 10, "abbrev": "TOR" },
              "awayTeam": { "id": 22, "abbrev": "EDM" }
            }
            """;

        var game = MapGameResponseToGame.Map(JsonNode.Parse(json), null, null);

        game.HasBeenPlayed.Should().BeFalse();
        game.HomeTeamId.Should().Be(10);
        game.AwayTeamId.Should().Be(22);
        game.SeasonStartYear.Should().Be(2024);
        game.HomeGoals.Should().Be(0);
        game.AwayGoals.Should().Be(0);
    }

    [TestMethod]
    public void MapGameResponse_PlayoffGameId_GameTypeIsPlayoff()
    {
        const string json = """
            {
              "id": 2023030111,
              "season": 20232024,
              "startTimeUTC": "2024-04-20T23:00:00Z",
              "gameState": "FUT",
              "homeTeam": { "id": 14, "abbrev": "TBL" },
              "awayTeam": { "id": 18, "abbrev": "NSH" }
            }
            """;

        var game = MapGameResponseToGame.Map(JsonNode.Parse(json), null, null);

        game.GameType.Should().Be(GameType.Playoff);
    }

    [TestMethod]
    public void MapGoalEvent_ParsesAllFields()
    {
        const string json = """
            {
              "plays": [{
                "eventId": 154, "typeCode": 505, "typeDescKey": "goal", "sortOrder": 151,
                "situationCode": "1551", "homeTeamDefendingSide": "left",
                "timeInPeriod": "09:48", "timeRemaining": "10:12",
                "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
                "pptReplayUrl": "https://wsr.nhle.com/sprites/20232024/2023020001/ev154.json",
                "details": {
                  "eventOwnerTeamId": 14, "scoringPlayerId": 8476453, "goalieInNetId": 8477424,
                  "assist1PlayerId": 8475167, "assist2PlayerId": 8478010,
                  "shotType": "slap", "zoneCode": "O", "xCoord": 50, "yCoord": -16,
                  "highlightClipSharingUrl": "https://nhl.com/video/nikita-kucherov-with-a-goal-vs-nashville-predators-6338805211112",
                  "highlightClip": 6338805211112
                }
              }]
            }
            """;

        var goal = MapGameEventsResponseToGameEvents.Map(JsonNode.Parse(json))
            .Events.OfType<Goal>().Single();

        goal.Id.Should().Be(154);
        goal.TypeCode.Should().Be(505);
        goal.SortOrder.Should().Be(151);
        goal.SituationCode.Should().Be(1551);
        goal.PeriodNumber.Should().Be(1);
        goal.PeriodType.Should().Be(PeriodType.Regulation);
        goal.EventTypeName.Should().Be("goal");
        goal.HomeTeamDefendingSide.Should().Be(HomeTeamDefendingSide.Left);
        goal.SecondsIntoPeriod.Should().Be(588);    // 09:48
        goal.SecondsLeftInPeriod.Should().Be(612);  // 10:12
        goal.ShotType.Should().Be(ShotType.Slap);
        goal.ScoringPlayerTeamId.Should().Be(14);
        goal.ScoringPlayerId.Should().Be(8476453);
        goal.GoalieId.Should().Be(8477424);
        goal.AssistOnePlayerId.Should().Be(8475167);
        goal.AssistTwoPlayerId.Should().Be(8478010);
        goal.Zone.Should().Be(Zone.Offensive);
        goal.XCoordinate.Should().Be(50);
        goal.YCoordinate.Should().Be(-16);
        goal.HighlightClipSharingUrl.Should().Be("https://nhl.com/video/nikita-kucherov-with-a-goal-vs-nashville-predators-6338805211112");
        goal.HighlightClipId.Should().Be(6338805211112L);
        goal.DiscreetClipId.Should().Be(-1);
        goal.PptReplayUrl.Should().Be("https://wsr.nhle.com/sprites/20232024/2023020001/ev154.json");
    }

    [TestMethod]
    public void MapPenaltyEvent_ParsesAllFields()
    {
        const string json = """
            {
              "plays": [{
                "eventId": 297, "typeCode": 509, "typeDescKey": "penalty", "sortOrder": 182,
                "situationCode": "1551", "homeTeamDefendingSide": "left",
                "timeInPeriod": "12:05", "timeRemaining": "07:55",
                "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
                "details": {
                  "eventOwnerTeamId": 14, "committedByPlayerId": 8480246, "drawnByPlayerId": 8481704,
                  "typeCode": "MIN", "descKey": "high-sticking", "duration": 2,
                  "zoneCode": "D", "xCoord": -94, "yCoord": -13
                }
              }]
            }
            """;

        var penalty = MapGameEventsResponseToGameEvents.Map(JsonNode.Parse(json))
            .Events.OfType<Penalty>().Single();

        penalty.Id.Should().Be(297);
        penalty.TypeCode.Should().Be(509);
        penalty.SortOrder.Should().Be(182);
        penalty.SituationCode.Should().Be(1551);
        penalty.PeriodNumber.Should().Be(1);
        penalty.PeriodType.Should().Be(PeriodType.Regulation);
        penalty.EventTypeName.Should().Be("penalty");
        penalty.HomeTeamDefendingSide.Should().Be(HomeTeamDefendingSide.Left);
        penalty.SecondsIntoPeriod.Should().Be(725);   // 12:05
        penalty.SecondsLeftInPeriod.Should().Be(475); // 07:55
        penalty.PenaltyType.Should().Be(PenaltyType.HighStick);
        penalty.PenaltySeverity.Should().Be(PenaltySeverity.Minor);
        penalty.CommittedByPlayerTeamId.Should().Be(14);
        penalty.CommittedByPlayerId.Should().Be(8480246);
        penalty.DrawnByPlayerId.Should().Be(8481704);
        penalty.Zone.Should().Be(Zone.Defensive);
        penalty.XCoordinate.Should().Be(-94);
        penalty.YCoordinate.Should().Be(-13);
        penalty.Duration.Should().Be(2);
    }

    [TestMethod]
    public void MapShotEvent_ParsesAllFields()
    {
        const string json = """
            {
              "plays": [{
                "eventId": 63, "typeCode": 506, "typeDescKey": "shot-on-goal", "sortOrder": 22,
                "situationCode": "1551", "homeTeamDefendingSide": "left",
                "timeInPeriod": "01:01", "timeRemaining": "18:59",
                "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
                "details": {
                  "eventOwnerTeamId": 14, "shootingPlayerId": 8478178, "goalieInNetId": 8477424,
                  "shotType": "wrist", "zoneCode": "O", "xCoord": 58, "yCoord": -25
                }
              }]
            }
            """;

        var shot = MapGameEventsResponseToGameEvents.Map(JsonNode.Parse(json))
            .Events.OfType<Shot>().Single();

        shot.Id.Should().Be(63);
        shot.TypeCode.Should().Be(506);
        shot.SortOrder.Should().Be(22);
        shot.SituationCode.Should().Be(1551);
        shot.PeriodNumber.Should().Be(1);
        shot.PeriodType.Should().Be(PeriodType.Regulation);
        shot.EventTypeName.Should().Be("shot-on-goal");
        shot.HomeTeamDefendingSide.Should().Be(HomeTeamDefendingSide.Left);
        shot.SecondsIntoPeriod.Should().Be(61);      // 01:01
        shot.SecondsLeftInPeriod.Should().Be(1139);  // 18:59
        shot.ShotType.Should().Be(ShotType.Wrist);
        shot.Zone.Should().Be(Zone.Offensive);
        shot.ShooterTeamId.Should().Be(14);
        shot.ShooterPlayerId.Should().Be(8478178);
        shot.GoalieId.Should().Be(8477424);
        shot.XCoordinate.Should().Be(58);
        shot.YCoordinate.Should().Be(-25);
    }

    [TestMethod]
    public void MapFaceoffEvent_ParsesAllFields()
    {
        const string json = """
            {
              "plays": [{
                "eventId": 101, "typeCode": 502, "typeDescKey": "faceoff", "sortOrder": 9,
                "situationCode": "1551", "homeTeamDefendingSide": "left",
                "timeInPeriod": "00:00", "timeRemaining": "20:00",
                "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
                "details": {
                  "eventOwnerTeamId": 18, "winningPlayerId": 8475158, "losingPlayerId": 8478519,
                  "xCoord": 0, "yCoord": 0, "zoneCode": "N"
                }
              }]
            }
            """;

        var faceoff = MapGameEventsResponseToGameEvents.Map(JsonNode.Parse(json))
            .Events.OfType<Faceoff>().Single();

        faceoff.Id.Should().Be(101);
        faceoff.TypeCode.Should().Be(502);
        faceoff.SortOrder.Should().Be(9);
        faceoff.SituationCode.Should().Be(1551);
        faceoff.PeriodNumber.Should().Be(1);
        faceoff.PeriodType.Should().Be(PeriodType.Regulation);
        faceoff.EventTypeName.Should().Be("faceoff");
        faceoff.HomeTeamDefendingSide.Should().Be(HomeTeamDefendingSide.Left);
        faceoff.SecondsIntoPeriod.Should().Be(0);
        faceoff.SecondsLeftInPeriod.Should().Be(1200); // 20:00
        faceoff.WinningTeamId.Should().Be(18);
        faceoff.WinningPlayerId.Should().Be(8475158);
        faceoff.LosingPlayerId.Should().Be(8478519);
        faceoff.XCoordinate.Should().Be(0);
        faceoff.YCoordinate.Should().Be(0);
        faceoff.Zone.Should().Be(Zone.Neutral);
    }

    [TestMethod]
    public void MapGameEvents_CountsEachEventType()
    {
        var events = MapGameEventsResponseToGameEvents.Map(JsonNode.Parse(PlayByPlayJson)).Events.ToList();

        events.Should().HaveCount(6);
        events.OfType<PeriodStart>().Should().HaveCount(1);
        events.OfType<Faceoff>().Should().HaveCount(1);
        events.OfType<Shot>().Should().HaveCount(1);
        events.OfType<Goal>().Should().HaveCount(1);
        events.OfType<Penalty>().Should().HaveCount(1);
        events.OfType<GameEnd>().Should().HaveCount(1);
    }

    [TestMethod]
    public void MapScheduleResponseToGameCount_ReturnsCountForSeason()
    {
        const string json = """
            {
              "data": [
                { "id": 20212022, "totalRegularSeasonGames": 1312 },
                { "id": 20222023, "totalRegularSeasonGames": 1312 },
                { "id": 20232024, "totalRegularSeasonGames": 1312 }
              ]
            }
            """;

        MapScheduleResponseToGameCount.Map(JsonNode.Parse(json), 20232024).Should().Be(1312);
    }

    [TestMethod]
    public void MapScheduleResponseToGameCount_UnknownSeasonReturnsZero()
    {
        const string json = """
            {
              "data": [
                { "id": 20232024, "totalRegularSeasonGames": 1312 }
              ]
            }
            """;

        MapScheduleResponseToGameCount.Map(JsonNode.Parse(json), 20192020).Should().Be(0);
    }

    [TestMethod]
    public void MapStandingsResponseToTeams_ParsesAllTeamFields()
    {
        const string json = """
            {
              "standings": [
                {
                  "seasonId": 20232024,
                  "teamName":       { "default": "Vancouver Canucks" },
                  "teamAbbrev":     { "default": "VAN" },
                  "teamCommonName": { "default": "Canucks" },
                  "teamLogo":       "https://assets.nhle.com/logos/nhl/svg/VAN_light.svg",
                  "divisionName":   "Pacific",
                  "divisionAbbrev": "P",
                  "conferenceName": "Western",
                  "conferenceAbbrev": "W",
                  "placeName": { "default": "Vancouver" }
                },
                {
                  "seasonId": 20232024,
                  "teamName":       { "default": "Boston Bruins" },
                  "teamAbbrev":     { "default": "BOS" },
                  "teamCommonName": { "default": "Bruins" },
                  "teamLogo":       "https://assets.nhle.com/logos/nhl/svg/BOS_20232024_light.svg",
                  "divisionName":   "Atlantic",
                  "divisionAbbrev": "A",
                  "conferenceName": "Eastern",
                  "conferenceAbbrev": "E",
                  "placeName": { "default": "Boston" }
                }
              ]
            }
            """;

        var teams = MapStandingsResponseToSeasonTeams.Map(JsonNode.Parse(json))!.ToList();

        teams.Should().HaveCount(2);

        var van = teams[0];
        van.SeasonStartYear.Should().Be(2023);
        van.Name.Should().Be("Vancouver Canucks");
        van.Abbreviation.Should().Be("VAN");
        van.CommonName.Should().Be("Canucks");
        van.LogoUri.Should().Be("https://assets.nhle.com/logos/nhl/svg/VAN_light.svg");
        van.Division.Should().Be("Pacific");
        van.DivisionAbbreviation.Should().Be("P");
        van.Conference.Should().Be("Western");
        van.ConferenceAbbreviation.Should().Be("W");
        van.PlaceName.Should().Be("Vancouver");

        var bos = teams[1];
        bos.Name.Should().Be("Boston Bruins");
        bos.Abbreviation.Should().Be("BOS");
        bos.Division.Should().Be("Atlantic");
        bos.Conference.Should().Be("Eastern");
    }
}
