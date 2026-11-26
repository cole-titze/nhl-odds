using Entities.ServiceModels.Mappers;
using FluentAssertions;

namespace BookmakerOddsGetter.Tests.Helpers;

[TestClass]
public class BookmakerOddsHelperTests
{
    [TestMethod]
    public void BuildGameInfoList_WithMatchingData_ShouldBuildCorrectList()
    {
        var games = new List<GameRef>
        {
            new GameRef(2024020001, 1, 2),
            new GameRef(2024020002, 3, 4),
        };
        var seasonTeams = new Dictionary<int, string>
        {
            { 1, "Boston Bruins" },
            { 2, "Montreal Canadiens" },
            { 3, "Toronto Maple Leafs" },
            { 4, "Ottawa Senators" },
        };
        var gameDates = new Dictionary<int, DateTime>
        {
            { 2024020001, new DateTime(2024, 1, 15) },
            { 2024020002, new DateTime(2024, 1, 16) },
        };

        var result = BookmakerOddsHelper.BuildGameInfoList(games, seasonTeams, gameDates);

        result.Should().HaveCount(2);
        result[0].GameId.Should().Be(2024020001);
        result[0].HomeTeamName.Should().Be("Boston Bruins");
        result[0].AwayTeamName.Should().Be("Montreal Canadiens");
        result[0].GameDateUTC.Should().Be(new DateTime(2024, 1, 15));
        result[1].GameId.Should().Be(2024020002);
        result[1].HomeTeamName.Should().Be("Toronto Maple Leafs");
    }

    [TestMethod]
    public void BuildGameInfoList_WithMissingTeam_ShouldSkipGame()
    {
        var games = new List<GameRef>
        {
            new GameRef(2024020001, 1, 999), // team 999 not in seasonTeams
        };
        var seasonTeams = new Dictionary<int, string>
        {
            { 1, "Boston Bruins" },
        };
        var gameDates = new Dictionary<int, DateTime>
        {
            { 2024020001, new DateTime(2024, 1, 15) },
        };

        var result = BookmakerOddsHelper.BuildGameInfoList(games, seasonTeams, gameDates);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void BuildGameInfoList_WithMissingDate_ShouldSkipGame()
    {
        var games = new List<GameRef>
        {
            new GameRef(2024020001, 1, 2),
        };
        var seasonTeams = new Dictionary<int, string>
        {
            { 1, "Boston Bruins" },
            { 2, "Montreal Canadiens" },
        };
        var gameDates = new Dictionary<int, DateTime>(); // no dates

        var result = BookmakerOddsHelper.BuildGameInfoList(games, seasonTeams, gameDates);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void BuildGameInfoList_WithEmptyGames_ShouldReturnEmpty()
    {
        var result = BookmakerOddsHelper.BuildGameInfoList(
            new List<GameRef>(),
            new Dictionary<int, string>(),
            new Dictionary<int, DateTime>());

        result.Should().BeEmpty();
    }
}
