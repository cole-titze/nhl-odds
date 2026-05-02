using System.Text.Json.Nodes;
using Entities.Models;
using Entities.Models.Teams;
using Entities.ServiceModels.Mappers;
using Entities.Types;
using Entities.Types.Enums;
using FluentAssertions;

namespace DataGetter.Tests.Mappers;

/// <summary>
/// Tests for player-related mappers:
///   MapPlayerResponseToPlayer, MapCurrentRosterResponseToGamePlayerStats,
///   MapGamePlayerStatsResponseToGamePlayerStats, MapAllTeamsResponseToTeams.
/// All JSON fixtures use real data captured from the NHL public API.
/// </summary>
[TestClass]
public class NhlApiPlayerParsingTests
{
    // ── MapPlayerResponseToPlayer ────────────────────────────────────────────

    // Real player data for Nikita Kucherov (id 8476453) captured 2025-05.
    private const string KucherovJson = """
        {
          "playerId": 8476453,
          "isActive": true,
          "firstName": { "default": "Nikita" },
          "lastName":  { "default": "Kucherov" },
          "headshot":  "https://assets.nhle.com/mugs/nhl/20242025/TBL/8476453.png",
          "heroImage": "https://assets.nhle.com/mugs/actionshots/1296x729/8476453.jpg",
          "heightInInches": 72,
          "weightInPounds": 173,
          "birthDate": "1993-06-17",
          "birthCity": { "default": "Maykop" },
          "birthCountry": "RUS",
          "inTop100AllTime": 0,
          "inHHOF": 0,
          "shopLink": "#TODO",
          "twitterLink": "#TODO",
          "watchLink": "#TODO",
          "playerSlug": "nikita-kucherov-8476453",
          "draftDetails": {
            "year": 2011,
            "teamAbbrev": "TBL",
            "round": 2,
            "pickInRound": 28,
            "overallPick": 58
          }
        }
        """;

    [TestMethod]
    public void MapPlayerResponse_ParsesCoreFields()
    {
        var player = MapPlayerResponseToPlayer.Map(JsonNode.Parse(KucherovJson), recentGameTeamId: 14);

        player.Id.Should().Be(8476453);
        player.FirstName.Should().Be("Nikita");
        player.LastName.Should().Be("Kucherov");
        player.IsActive.Should().BeTrue();
        player.CurrentTeamId.Should().Be(14);
        player.HeightInInches.Should().Be(72);
        player.WeightInPounds.Should().Be(173);
        player.BirthDate.Should().Be(new DateTime(1993, 6, 17));
        player.BirthCity.Should().Be("Maykop");
        player.BirthStateProvince.Should().BeEmpty();
        player.BirthCountry.Should().Be("RUS");
        player.IsInTopOneHundredAllTime.Should().BeFalse();
        player.IsInHallOfFame.Should().BeFalse();
        player.PlayerSlug.Should().Be("nikita-kucherov-8476453");
    }

    [TestMethod]
    public void MapPlayerResponse_ParsesDraftDetails()
    {
        var player = MapPlayerResponseToPlayer.Map(JsonNode.Parse(KucherovJson), recentGameTeamId: 14);

        player.PlayerDraftDetails.Should().NotBeNull();
        player.PlayerDraftDetails!.Year.Should().Be(2011);
        player.PlayerDraftDetails.TeamAbbrev.Should().Be("TBL");
        player.PlayerDraftDetails.Round.Should().Be(2);
        player.PlayerDraftDetails.PickInRound.Should().Be(28);
        player.PlayerDraftDetails.OverallPick.Should().Be(58);
    }

    [TestMethod]
    public void MapPlayerResponse_NullDraftDetails_ReturnsNullDraftDetails()
    {
        const string json = """
            {
              "playerId": 8480801,
              "isActive": true,
              "firstName": { "default": "Brandon" },
              "lastName":  { "default": "Hagel" },
              "headshot":  "https://assets.nhle.com/mugs/nhl/20242025/TBL/8480801.png",
              "heroImage": "https://assets.nhle.com/mugs/actionshots/1296x729/8480801.jpg",
              "heightInInches": 71,
              "weightInPounds": 186,
              "birthDate": "1998-08-27",
              "birthCity":          { "default": "Moose Jaw" },
              "birthStateProvince": { "default": "SK" },
              "birthCountry": "CAN",
              "inTop100AllTime": 0,
              "inHHOF": 0,
              "shopLink":   "#TODO",
              "twitterLink": "#TODO",
              "watchLink":   "#TODO",
              "playerSlug": "brandon-hagel-8480801"
            }
            """;

        var player = MapPlayerResponseToPlayer.Map(JsonNode.Parse(json), recentGameTeamId: 14);

        player.PlayerDraftDetails.Should().BeNull();
        player.BirthStateProvince.Should().Be("SK");
    }

    // ── MapCurrentRosterResponseToGamePlayerStats ────────────────────────────

    // Real TBL roster data (futures/pre-game path, all stats zeroed out).
    private const string TblRosterJson = """
        {
          "forwards": [
            { "id": 8477416, "positionCode": "R" },
            { "id": 8476453, "positionCode": "R" },
            { "id": 8480801, "positionCode": "L" }
          ],
          "defensemen": [
            { "id": 8476897, "positionCode": "D" },
            { "id": 8481600, "positionCode": "D" }
          ],
          "goalies": [
            { "id": 8477992, "positionCode": "G" }
          ]
        }
        """;

    private const string NshRosterJson = """
        {
          "forwards": [
            { "id": 8476925, "positionCode": "C" }
          ],
          "defensemen": [
            { "id": 8477493, "positionCode": "D" }
          ],
          "goalies": [
            { "id": 8471306, "positionCode": "G" }
          ]
        }
        """;

    [TestMethod]
    public void MapCurrentRoster_PopulatesPlayerIds()
    {
        var roster = MapCurrentRosterResponseToGamePlayerStats.Map(
            JsonNode.Parse(TblRosterJson), JsonNode.Parse(NshRosterJson),
            homeTeamId: 14, awayTeamId: 18);

        roster.HomeTeamForwards.Should().Contain(p => p.PlayerId == 8477416);
        roster.HomeTeamForwards.Should().Contain(p => p.PlayerId == 8476453);
        roster.HomeTeamDefensemen.Should().Contain(p => p.PlayerId == 8476897);
        roster.HomeTeamGoalies.Should().Contain(p => p.PlayerId == 8477992);

        roster.AwayTeamForwards.Should().Contain(p => p.PlayerId == 8476925);
        roster.AwayTeamDefensemen.Should().Contain(p => p.PlayerId == 8477493);
        roster.AwayTeamGoalies.Should().Contain(p => p.PlayerId == 8471306);
    }

    [TestMethod]
    public void MapCurrentRoster_AssignsTeamIds()
    {
        var roster = MapCurrentRosterResponseToGamePlayerStats.Map(
            JsonNode.Parse(TblRosterJson), JsonNode.Parse(NshRosterJson),
            homeTeamId: 14, awayTeamId: 18);

        roster.HomeTeamForwards.OfType<GameSkaterStats>().Should().OnlyContain(p => p.TeamId == 14);
        roster.HomeTeamGoalies.OfType<GameGoalieStats>().Should().OnlyContain(p => p.TeamId == 14);
        roster.AwayTeamForwards.OfType<GameSkaterStats>().Should().OnlyContain(p => p.TeamId == 18);
    }

    [TestMethod]
    public void MapCurrentRoster_ZeroesOutStats()
    {
        var roster = MapCurrentRosterResponseToGamePlayerStats.Map(
            JsonNode.Parse(TblRosterJson), JsonNode.Parse(NshRosterJson),
            homeTeamId: 14, awayTeamId: 18);

        var skater = roster.HomeTeamForwards.OfType<GameSkaterStats>().First();
        skater.Goals.Should().Be(0);
        skater.Assists.Should().Be(0);
        skater.ShotsOnGoal.Should().Be(0);
        skater.TimeOnIceSeconds.Should().Be(0);

        var goalie = roster.HomeTeamGoalies.OfType<GameGoalieStats>().First();
        goalie.EvenStrengthShotsSaved.Should().Be(0);
        goalie.PowerPlayShotsSaved.Should().Be(0);
        goalie.TimeOnIceSeconds.Should().Be(0);
        goalie.IsStarter.Should().BeFalse();
    }

    [TestMethod]
    public void MapCurrentRoster_ParsesPositions()
    {
        var roster = MapCurrentRosterResponseToGamePlayerStats.Map(
            JsonNode.Parse(TblRosterJson), JsonNode.Parse(NshRosterJson),
            homeTeamId: 14, awayTeamId: 18);

        var goalie = roster.HomeTeamGoalies.OfType<GameGoalieStats>().First(p => p.PlayerId == 8477992);
        goalie.Position.Should().Be(POSITION.Goalie);
    }

    // ── MapGamePlayerStatsResponseToGamePlayerStats ──────────────────────────

    // Minimal boxscore section with playerByGameStats — real data from game 2023020001.
    private const string BoxscoreWithPlayerStatsJson = """
        {
          "id": 2023020001,
          "homeTeam": { "id": 14 },
          "awayTeam": { "id": 18 },
          "playerByGameStats": {
            "homeTeam": {
              "forwards": [
                {
                  "playerId": 8476822,
                  "goals": 0, "assists": 1, "sog": 1, "blockedShots": 2,
                  "pim": 0, "powerPlayGoals": 0, "plusMinus": 0,
                  "faceoffWinningPctg": 0.692308,
                  "hits": 1, "giveaways": 0, "takeaways": 0,
                  "toi": "14:01", "position": "C"
                }
              ],
              "defense": [
                {
                  "playerId": 8471685,
                  "goals": 0, "assists": 0, "sog": 2, "blockedShots": 1,
                  "pim": 2, "powerPlayGoals": 0, "plusMinus": 1,
                  "faceoffWinningPctg": 0.0,
                  "hits": 2, "giveaways": 0, "takeaways": 1,
                  "toi": "22:14", "position": "D"
                }
              ],
              "goalies": [
                {
                  "playerId": 8477992,
                  "evenStrengthShotsAgainst": "21/23",
                  "powerPlayShotsAgainst": "6/7",
                  "shortHandedShotsAgainst": null,
                  "evenStrengthGoalsAgainst": 2,
                  "powerPlayGoalsAgainst": 1,
                  "shortHandedGoalsAgainst": null,
                  "toi": "60:00",
                  "starter": true
                }
              ]
            },
            "awayTeam": {
              "forwards": [
                {
                  "playerId": 8478864,
                  "goals": 1, "assists": 0, "sog": 3, "blockedShots": 0,
                  "pim": 0, "powerPlayGoals": 0, "plusMinus": 1,
                  "faceoffWinningPctg": 0.0,
                  "hits": 0, "giveaways": 1, "takeaways": 0,
                  "toi": "16:33", "position": "L"
                }
              ],
              "defense": [],
              "goalies": [
                {
                  "playerId": 8471306,
                  "evenStrengthShotsAgainst": "28/32",
                  "powerPlayShotsAgainst": "3/3",
                  "shortHandedShotsAgainst": null,
                  "evenStrengthGoalsAgainst": 4,
                  "powerPlayGoalsAgainst": 0,
                  "shortHandedGoalsAgainst": null,
                  "toi": "60:00",
                  "starter": true
                }
              ]
            }
          }
        }
        """;

    // Real right-rail gameInfo section from game 2023020001.
    private const string RightRailGameInfoJson = """
        {
          "gameInfo": {
            "homeTeam": { "headCoach": { "default": "Jon Cooper" } },
            "awayTeam": { "headCoach": { "default": "Andrew Brunette" } },
            "referees": [
              { "default": "Chris Rooney" },
              { "default": "Jake Brenk" }
            ],
            "linesmen": [
              { "default": "Shandor Alphonso" },
              { "default": "David Brisebois" }
            ]
          }
        }
        """;

    [TestMethod]
    public void MapGamePlayerStats_ParsesSkaterStats()
    {
        var roster = MapGamePlayerStatsResponseToGamePlayerStats.Map(
            JsonNode.Parse(BoxscoreWithPlayerStatsJson),
            JsonNode.Parse(RightRailGameInfoJson));

        var forward = roster.HomeTeamForwards.OfType<GameSkaterStats>()
            .Single(p => p.PlayerId == 8476822);

        forward.TeamId.Should().Be(14);
        forward.Assists.Should().Be(1);
        forward.ShotsOnGoal.Should().Be(1);
        forward.BlockedShots.Should().Be(2);
        forward.Hits.Should().Be(1);
        forward.FaceOffWinningPctg.Should().BeApproximately(0.692308, 0.000001);
        forward.TimeOnIceSeconds.Should().Be(841); // 14:01 = 14*60+1
        forward.Position.Should().Be(POSITION.Center);
    }

    [TestMethod]
    public void MapGamePlayerStats_ParsesGoalieStats()
    {
        var roster = MapGamePlayerStatsResponseToGamePlayerStats.Map(
            JsonNode.Parse(BoxscoreWithPlayerStatsJson),
            JsonNode.Parse(RightRailGameInfoJson));

        var goalie = roster.HomeTeamGoalies.OfType<GameGoalieStats>()
            .Single(p => p.PlayerId == 8477992);

        goalie.TeamId.Should().Be(14);
        goalie.EvenStrengthShotsSaved.Should().Be(21); // "21/23" → 21 saved
        goalie.PowerPlayShotsSaved.Should().Be(6);     // "6/7"  → 6 saved
        goalie.ShortHandedShotsSaved.Should().Be(0);   // null   → 0
        goalie.EvenStrengthGoalsAllowed.Should().Be(2);
        goalie.PowerPlayGoalsAllowed.Should().Be(1);
        goalie.ShortHandedGoalsAllowed.Should().Be(0);
        goalie.TimeOnIceSeconds.Should().Be(3600);     // 60:00
        goalie.IsStarter.Should().BeTrue();
    }

    [TestMethod]
    public void MapGamePlayerStats_ParsesCoaches()
    {
        var roster = MapGamePlayerStatsResponseToGamePlayerStats.Map(
            JsonNode.Parse(BoxscoreWithPlayerStatsJson),
            JsonNode.Parse(RightRailGameInfoJson));

        roster.HomeTeamCoach.Name.Should().Be("Jon Cooper");
        roster.AwayTeamCoach.Name.Should().Be("Andrew Brunette");
    }

    [TestMethod]
    public void MapGamePlayerStats_ParsesOfficials()
    {
        var roster = MapGamePlayerStatsResponseToGamePlayerStats.Map(
            JsonNode.Parse(BoxscoreWithPlayerStatsJson),
            JsonNode.Parse(RightRailGameInfoJson));

        var referees = roster.Referees.Select(r => r.Name).ToList();
        referees.Should().Contain("Chris Rooney");
        referees.Should().Contain("Jake Brenk");

        var linesmen = roster.Linesmen.Select(l => l.Name).ToList();
        linesmen.Should().Contain("Shandor Alphonso");
        linesmen.Should().Contain("David Brisebois");
    }

    [TestMethod]
    public void MapGamePlayerStats_AwayTeamForwardParsed()
    {
        var roster = MapGamePlayerStatsResponseToGamePlayerStats.Map(
            JsonNode.Parse(BoxscoreWithPlayerStatsJson),
            JsonNode.Parse(RightRailGameInfoJson));

        var forward = roster.AwayTeamForwards.OfType<GameSkaterStats>()
            .Single(p => p.PlayerId == 8478864);

        forward.TeamId.Should().Be(18);
        forward.Goals.Should().Be(1);
        forward.TimeOnIceSeconds.Should().Be(993); // 16:33 = 16*60+33
    }

    // ── MapAllTeamsResponseToTeams ───────────────────────────────────────────

    // Real response from https://api.nhle.com/stats/rest/en/team (first two entries).
    private const string AllTeamsJson = """
        {
          "data": [
            { "id": 32, "franchiseId": 27, "leagueId": 133, "triCode": "QUE" },
            { "id": 8,  "franchiseId": 1,  "leagueId": 133, "triCode": "MTL" }
          ],
          "total": 2
        }
        """;

    [TestMethod]
    public void MapAllTeams_ParsesTeamFields()
    {
        var teams = MapAllTeamsResponseToTeams.Map(JsonNode.Parse(AllTeamsJson)).ToList();

        teams.Should().HaveCount(2);

        var que = teams.Single(t => t.Id == 32);
        que.FranchiseId.Should().Be(27);
        que.LeagueId.Should().Be(133);
        que.Abbreviation.Should().Be("QUE");

        var mtl = teams.Single(t => t.Id == 8);
        mtl.FranchiseId.Should().Be(1);
        mtl.LeagueId.Should().Be(133);
        mtl.Abbreviation.Should().Be("MTL");
    }
}
