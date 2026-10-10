using DataGetter.BusinessLogic;
using FluentAssertions;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class GameDetailParserTests
{
    [TestMethod]
    public void ParseRosterSpots_ReadsTeamSweaterAndPosition()
    {
        var json = """
            { "rosterSpots": [
              { "teamId": 55, "playerId": 8474586, "sweaterNumber": 7, "positionCode": "R" },
              { "teamId": 17, "playerId": 8477946, "positionCode": "C" },
              { "teamId": 55, "playerId": 8474586, "sweaterNumber": 7, "positionCode": "R" } ] }
            """;

        var spots = GameDetailParser.ParseRosterSpots(2026020066, json);

        spots.Should().HaveCount(2);
        spots[0].Should().Match<Entities.DbModels.DbGameRosterSpot>(s =>
            s.GameId == 2026020066 && s.TeamId == 55 && s.SweaterNumber == 7 && s.PositionCode == "R");
        spots[1].SweaterNumber.Should().BeNull();
    }

    [TestMethod]
    public void ParseShifts_ConvertsTimesAndKeepsGoalMarkers()
    {
        var json = """
            { "data": [
              { "id": 16694829, "detailCode": 0, "duration": "00:40", "endTime": "01:25", "eventDescription": null,
                "eventDetails": null, "eventNumber": 65, "period": 1, "playerId": 8474586, "shiftNumber": 1,
                "startTime": "00:45", "teamId": 55, "typeCode": 517 },
              { "id": 16694900, "detailCode": 803, "duration": null, "endTime": "12:34", "eventDescription": "PPG",
                "eventDetails": "Assists: A, B", "eventNumber": 300, "period": 2, "playerId": 8478398, "shiftNumber": 0,
                "startTime": "12:34", "teamId": 52, "typeCode": 505 } ], "total": 2 }
            """;

        var shifts = GameDetailParser.ParseShifts(2026020066, json);

        shifts.Should().HaveCount(2);
        shifts[0].Should().Match<Entities.DbModels.DbGameShift>(s =>
            s.Id == 16694829 && s.StartSeconds == 45 && s.EndSeconds == 85 && s.DurationSeconds == 40 && s.TypeCode == 517);
        shifts[1].Should().Match<Entities.DbModels.DbGameShift>(s =>
            s.DurationSeconds == null && s.StartSeconds == 754 && s.EventDescription == "PPG" && s.DetailCode == 803);
    }

    [TestMethod]
    public void ParseReplay_OneRowPerObjectPerFrame()
    {
        var json = """
            [
              { "timeStamp": 100, "onIce": {
                "1": { "id": 1, "playerId": "", "x": 57.5, "y": 869.9, "sweaterNumber": "", "teamId": "", "teamAbbrev": "" },
                "52081": { "id": 52081, "playerId": 8478398, "x": 57.8, "y": 788.9, "sweaterNumber": 81, "teamId": 52, "teamAbbrev": "WPG" } } },
              { "timeStamp": 101, "onIce": {
                "1": { "id": 1, "playerId": "", "x": 60.3, "y": 873.8, "sweaterNumber": "", "teamId": "", "teamAbbrev": "" },
                "": {} } }
            ]
            """;

        var positions = GameDetailParser.ParseReplay(2026020069, 1034, json);

        // The empty {} entry is skipped
        positions.Should().HaveCount(3);
        var puck = positions[0];
        puck.Should().Match<Entities.DbModels.DbGoalReplayPosition>(p =>
            p.Frame == 0 && p.TrackId == 1 && p.TimeStamp == 100 && p.PlayerId == null && p.TeamId == null && p.SweaterNumber == null);
        positions[1].Should().Match<Entities.DbModels.DbGoalReplayPosition>(p =>
            p.PlayerId == 8478398 && p.TeamId == 52 && p.SweaterNumber == 81 && Math.Abs(p.X - 57.8f) < 0.001);
        positions[2].Frame.Should().Be(1);
    }

    [TestMethod]
    public void Seconds_ParsesMinutesAndSeconds()
    {
        GameDetailParser.Seconds("12:34").Should().Be(754);
        GameDetailParser.Seconds("00:00").Should().Be(0);
        GameDetailParser.Seconds(null).Should().BeNull();
        GameDetailParser.Seconds("").Should().BeNull();
    }
}