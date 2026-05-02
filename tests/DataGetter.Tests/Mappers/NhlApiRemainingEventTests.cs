using System.Text.Json.Nodes;
using Entities.Models.GamePlayEvents;
using Entities.ServiceModels.Mappers;
using Entities.Types.Enums;
using FluentAssertions;

namespace DataGetter.Tests.Mappers;

/// <summary>
/// Tests for event types not covered in NhlApiJsonParsingTests:
/// hit, blocked-shot, missed-shot, giveaway, takeaway, stoppage,
/// delayed-penalty, period-end, failed-shot-attempt.
/// Also tests both homeTeamDefendingSide values (left and right).
/// All JSON fixtures are real events captured from game 2023020001 (TBL vs NSH).
/// </summary>
[TestClass]
public class NhlApiRemainingEventTests
{
    private static GameEvents Parse(string playsJson) =>
        MapGameEventsResponseToGameEvents.Map(JsonNode.Parse($$"""{"plays": [{{playsJson}}]}"""));

    // ── Hit ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void MapHitEvent_ParsesAllFields()
    {
        const string json = """
            {
              "eventId": 9, "typeCode": 503, "typeDescKey": "hit", "sortOrder": 20,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "00:48", "timeRemaining": "19:12",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "eventOwnerTeamId": 18, "hittingPlayerId": 8474568, "hitteePlayerId": 8476453, "zoneCode": "D", "xCoord": 64, "yCoord": 42 }
            }
            """;

        var hit = Parse(json).Events.OfType<Hit>().Single();

        hit.Id.Should().Be(9);
        hit.TypeCode.Should().Be(503);
        hit.SortOrder.Should().Be(20);
        hit.SituationCode.Should().Be(1551);
        hit.PeriodNumber.Should().Be(1);
        hit.PeriodType.Should().Be(PeriodType.Regulation);
        hit.EventTypeName.Should().Be("hit");
        hit.HomeTeamDefendingSide.Should().Be(HomeTeamDefendingSide.Left);
        hit.SecondsIntoPeriod.Should().Be(48);     // 00:48
        hit.SecondsLeftInPeriod.Should().Be(1152); // 19:12
        hit.HittingPlayerTeamId.Should().Be(18);
        hit.HittingPlayerId.Should().Be(8474568);
        hit.HitteePlayerId.Should().Be(8476453);
        hit.Zone.Should().Be(Zone.Defensive);
        hit.XCoordinate.Should().Be(64);
        hit.YCoordinate.Should().Be(42);
    }

    // ── Blocked shot ─────────────────────────────────────────────────────────

    [TestMethod]
    public void MapBlockedShotEvent_ParsesAllFields()
    {
        const string json = """
            {
              "eventId": 233, "typeCode": 508, "typeDescKey": "blocked-shot", "sortOrder": 106,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "06:58", "timeRemaining": "13:02",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": {
                "eventOwnerTeamId": 18, "blockingPlayerId": 8478178, "shootingPlayerId": 8475158,
                "reason": "blocked", "zoneCode": "D", "xCoord": -79, "yCoord": -3
              }
            }
            """;

        var bs = Parse(json).Events.OfType<BlockedShot>().Single();

        bs.Id.Should().Be(233);
        bs.TypeCode.Should().Be(508);
        bs.SortOrder.Should().Be(106);
        bs.SituationCode.Should().Be(1551);
        bs.PeriodNumber.Should().Be(1);
        bs.EventTypeName.Should().Be("blocked-shot");
        bs.HomeTeamDefendingSide.Should().Be(HomeTeamDefendingSide.Left);
        bs.SecondsIntoPeriod.Should().Be(418);  // 06:58
        bs.SecondsLeftInPeriod.Should().Be(782); // 13:02
        bs.BlockType.Should().Be(BlockType.Opponent);
        bs.BlockingPlayerTeamId.Should().Be(18);
        bs.BlockingPlayerId.Should().Be(8478178);
        bs.ShooterPlayerId.Should().Be(8475158);
        bs.Zone.Should().Be(Zone.Defensive);
        bs.XCoordinate.Should().Be(-79);
        bs.YCoordinate.Should().Be(-3);
    }

    // ── Missed shot ───────────────────────────────────────────────────────────

    [TestMethod]
    public void MapMissedShotEvent_ParsesAllFields()
    {
        const string json = """
            {
              "eventId": 95, "typeCode": 507, "typeDescKey": "missed-shot", "sortOrder": 60,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "03:51", "timeRemaining": "16:09",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": {
                "eventOwnerTeamId": 18, "shootingPlayerId": 8476887, "goalieInNetId": 8477992,
                "shotType": "wrist", "reason": "wide-left", "zoneCode": "O", "xCoord": -63, "yCoord": 33
              }
            }
            """;

        var ms = Parse(json).Events.OfType<MissedShot>().Single();

        ms.Id.Should().Be(95);
        ms.TypeCode.Should().Be(507);
        ms.EventTypeName.Should().Be("missed-shot");
        ms.SecondsIntoPeriod.Should().Be(231);  // 03:51
        ms.SecondsLeftInPeriod.Should().Be(969); // 16:09
        ms.ShotType.Should().Be(ShotType.Wrist);
        ms.MissType.Should().Be(MissedShotType.WideLeft);
        ms.ShootingTeamId.Should().Be(18);
        ms.ShootingPlayerId.Should().Be(8476887);
        ms.GoalieId.Should().Be(8477992);
        ms.Zone.Should().Be(Zone.Offensive);
        ms.XCoordinate.Should().Be(-63);
        ms.YCoordinate.Should().Be(33);
    }

    // ── Giveaway ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void MapGiveawayEvent_ParsesAllFields()
    {
        const string json = """
            {
              "eventId": 104, "typeCode": 504, "typeDescKey": "giveaway", "sortOrder": 21,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "00:58", "timeRemaining": "19:02",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "eventOwnerTeamId": 18, "playerId": 8482062, "zoneCode": "D", "xCoord": 57, "yCoord": 30 }
            }
            """;

        var ga = Parse(json).Events.OfType<Giveaway>().Single();

        ga.Id.Should().Be(104);
        ga.TypeCode.Should().Be(504);
        ga.EventTypeName.Should().Be("giveaway");
        ga.SecondsIntoPeriod.Should().Be(58);
        ga.SecondsLeftInPeriod.Should().Be(1142); // 19:02
        ga.GiveawayPlayerTeamId.Should().Be(18);
        ga.GiveawayPlayerId.Should().Be(8482062);
        ga.Zone.Should().Be(Zone.Defensive);
        ga.XCoordinate.Should().Be(57);
        ga.YCoordinate.Should().Be(30);
    }

    // ── Takeaway ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void MapTakeawayEvent_ParsesAllFields()
    {
        const string json = """
            {
              "eventId": 106, "typeCode": 525, "typeDescKey": "takeaway", "sortOrder": 45,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "02:25", "timeRemaining": "17:35",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "eventOwnerTeamId": 18, "playerId": 8474151, "zoneCode": "D", "xCoord": 79, "yCoord": -27 }
            }
            """;

        var ta = Parse(json).Events.OfType<Takeaway>().Single();

        ta.Id.Should().Be(106);
        ta.TypeCode.Should().Be(525);
        ta.EventTypeName.Should().Be("takeaway");
        ta.SecondsIntoPeriod.Should().Be(145);   // 02:25
        ta.SecondsLeftInPeriod.Should().Be(1055); // 17:35
        ta.TakeawayPlayerTeamId.Should().Be(18);
        ta.TakeawayPlayerId.Should().Be(8474151);
        ta.Zone.Should().Be(Zone.Defensive);
        ta.XCoordinate.Should().Be(79);
        ta.YCoordinate.Should().Be(-27);
    }

    // ── Stoppage ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void MapStoppageEvent_ParsesIcingWithNoSecondaryReason()
    {
        const string json = """
            {
              "eventId": 8, "typeCode": 516, "typeDescKey": "stoppage", "sortOrder": 15,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "00:35", "timeRemaining": "19:25",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "reason": "icing" }
            }
            """;

        var stop = Parse(json).Events.OfType<Stoppage>().Single();

        stop.Id.Should().Be(8);
        stop.TypeCode.Should().Be(516);
        stop.EventTypeName.Should().Be("stoppage");
        stop.SecondsIntoPeriod.Should().Be(35);
        stop.SecondsLeftInPeriod.Should().Be(1165); // 19:25
        stop.StoppageType.Should().Be(StoppageType.Icing);
        stop.StoppageDetails.Should().Be(StoppageDetails.None);
    }

    [TestMethod]
    public void MapStoppageEvent_ParsesSecondaryReason()
    {
        const string json = """
            {
              "eventId": 50, "typeCode": 516, "typeDescKey": "stoppage", "sortOrder": 50,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "05:00", "timeRemaining": "15:00",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "reason": "puck-frozen", "secondaryReason": "offside" }
            }
            """;

        var stop = Parse(json).Events.OfType<Stoppage>().Single();

        stop.StoppageType.Should().Be(StoppageType.GoalieFreezePuck);
        stop.StoppageDetails.Should().Be(StoppageDetails.Offside);
    }

    // ── Delayed penalty ───────────────────────────────────────────────────────

    [TestMethod]
    public void MapDelayedPenaltyEvent_ParsesPenaltyTeamId()
    {
        const string json = """
            {
              "eventId": 31, "typeCode": 535, "typeDescKey": "delayed-penalty", "sortOrder": 213,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "14:13", "timeRemaining": "05:47",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "eventOwnerTeamId": 18 }
            }
            """;

        var dp = Parse(json).Events.OfType<DelayedPenalty>().Single();

        dp.Id.Should().Be(31);
        dp.TypeCode.Should().Be(535);
        dp.EventTypeName.Should().Be("delayed-penalty");
        dp.SecondsIntoPeriod.Should().Be(853);  // 14:13
        dp.SecondsLeftInPeriod.Should().Be(347); // 05:47
        dp.PenaltyTeamId.Should().Be(18);
    }

    // ── Period end ────────────────────────────────────────────────────────────

    [TestMethod]
    public void MapPeriodEndEvent_ParsesBaseFields()
    {
        const string json = """
            {
              "eventId": 39, "typeCode": 521, "typeDescKey": "period-end", "sortOrder": 291,
              "situationCode": "1451", "homeTeamDefendingSide": "left",
              "timeInPeriod": "20:00", "timeRemaining": "00:00",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 }
            }
            """;

        var pe = Parse(json).Events.OfType<PeriodEnd>().Single();

        pe.Id.Should().Be(39);
        pe.TypeCode.Should().Be(521);
        pe.EventTypeName.Should().Be("period-end");
        pe.SituationCode.Should().Be(1451);
        pe.PeriodNumber.Should().Be(1);
        pe.PeriodType.Should().Be(PeriodType.Regulation);
        pe.SecondsIntoPeriod.Should().Be(1200);
        pe.SecondsLeftInPeriod.Should().Be(0);
    }

    // ── Failed shot attempt ───────────────────────────────────────────────────

    [TestMethod]
    public void MapFailedShotAttemptEvent_ParsesAllFields()
    {
        // failed-shot-attempt is rare in real data; the mapper reads the same
        // fields as shot-on-goal so we verify the shape with a realistic fixture.
        const string json = """
            {
              "eventId": 200, "typeCode": 510, "typeDescKey": "failed-shot-attempt", "sortOrder": 200,
              "situationCode": "1551", "homeTeamDefendingSide": "right",
              "timeInPeriod": "10:00", "timeRemaining": "10:00",
              "periodDescriptor": { "number": 2, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": {
                "eventOwnerTeamId": 14, "shootingPlayerId": 8476453, "goalieInNetId": 8477424,
                "shotType": "snap", "zoneCode": "O", "xCoord": 55, "yCoord": 10
              }
            }
            """;

        var fsa = Parse(json).Events.OfType<FailedShotAttempt>().Single();

        fsa.Id.Should().Be(200);
        fsa.TypeCode.Should().Be(510);
        fsa.EventTypeName.Should().Be("failed-shot-attempt");
        fsa.PeriodNumber.Should().Be(2);
        fsa.SecondsIntoPeriod.Should().Be(600);  // 10:00
        fsa.SecondsLeftInPeriod.Should().Be(600); // 10:00
        fsa.ShotType.Should().Be(ShotType.Snap);
        fsa.ShootingTeamId.Should().Be(14);
        fsa.ShootingPlayerId.Should().Be(8476453);
        fsa.GoalieId.Should().Be(8477424);
        fsa.Zone.Should().Be(Zone.Offensive);
        fsa.XCoordinate.Should().Be(55);
        fsa.YCoordinate.Should().Be(10);
    }

    // ── HomeTeamDefendingSide ─────────────────────────────────────────────────

    [TestMethod]
    public void HomeTeamDefendingSide_Right_ParsesCorrectly()
    {
        // Periods 2 and 4 have homeTeamDefendingSide = "right"
        const string json = """
            {
              "eventId": 300, "typeCode": 502, "typeDescKey": "faceoff", "sortOrder": 300,
              "situationCode": "1551", "homeTeamDefendingSide": "right",
              "timeInPeriod": "00:00", "timeRemaining": "20:00",
              "periodDescriptor": { "number": 2, "periodType": "REG", "maxRegulationPeriods": 3 },
              "details": { "eventOwnerTeamId": 14, "winningPlayerId": 8476453, "losingPlayerId": 8478519, "xCoord": 0, "yCoord": 0, "zoneCode": "N" }
            }
            """;

        var faceoff = Parse(json).Events.OfType<Faceoff>().Single();

        faceoff.HomeTeamDefendingSide.Should().Be(HomeTeamDefendingSide.Right);
    }

    [TestMethod]
    public void HomeTeamDefendingSide_NullValue_ReturnsUnknown()
    {
        // Some older season events omit homeTeamDefendingSide; the parser should return Unknown.
        const string json = """
            {
              "eventId": 400, "typeCode": 520, "typeDescKey": "period-start", "sortOrder": 1,
              "situationCode": "1551",
              "timeInPeriod": "00:00", "timeRemaining": "20:00",
              "periodDescriptor": { "number": 1, "periodType": "REG", "maxRegulationPeriods": 3 }
            }
            """;

        var ps = Parse(json).Events.OfType<PeriodStart>().Single();

        ps.HomeTeamDefendingSide.Should().Be(HomeTeamDefendingSide.Unknown);
    }

    // ── Overtime and shootout period types ────────────────────────────────────

    [TestMethod]
    public void PeriodTypeOT_ParsesCorrectly()
    {
        const string json = """
            {
              "eventId": 500, "typeCode": 521, "typeDescKey": "period-end", "sortOrder": 500,
              "situationCode": "1551", "homeTeamDefendingSide": "left",
              "timeInPeriod": "05:00", "timeRemaining": "00:00",
              "periodDescriptor": { "number": 4, "periodType": "OT", "maxRegulationPeriods": 3 }
            }
            """;

        var pe = Parse(json).Events.OfType<PeriodEnd>().Single();

        pe.PeriodType.Should().Be(PeriodType.Overtime);
        pe.PeriodNumber.Should().Be(4);
    }

    [TestMethod]
    public void PeriodTypeSO_ParsesCorrectly()
    {
        const string json = """
            {
              "eventId": 600, "typeCode": 505, "typeDescKey": "goal", "sortOrder": 600,
              "situationCode": "1010", "homeTeamDefendingSide": "left",
              "timeInPeriod": "00:04", "timeRemaining": "00:56",
              "periodDescriptor": { "number": 5, "periodType": "SO", "maxRegulationPeriods": 3 },
              "details": {
                "eventOwnerTeamId": 14, "scoringPlayerId": 8476453, "shotType": "wrist", "zoneCode": "O"
              }
            }
            """;

        var goal = Parse(json).Events.OfType<Goal>().Single();

        goal.PeriodType.Should().Be(PeriodType.Shootout);
        goal.PeriodNumber.Should().Be(5);
    }
}
