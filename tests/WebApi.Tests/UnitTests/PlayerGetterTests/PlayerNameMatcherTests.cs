using Entities.Models.Web;
using Entities.Types;
using FluentAssertions;
using WebApi.BusinessLogic.PlayerGetter;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.PlayerGetterTests;

[TestClass]
public class PlayerNameMatcherTests
{
    private static readonly List<PlayerName> Players =
    [
        new() { Id = 1, FirstName = "Connor", LastName = "McDavid" },
        new() { Id = 2, FirstName = "Tim", LastName = "Stützle" },
        new() { Id = 3, FirstName = "Oliver", LastName = "Ekman-Larsson" },
        new() { Id = 4, FirstName = "Ryan", LastName = "O'Reilly" },
        new() { Id = 5, FirstName = "Connor", LastName = "Bedard" },
        new() { Id = 6, FirstName = "Connor", LastName = "Murphy" },
        new() { Id = 7, FirstName = "Jaromír", LastName = "Jágr" },
    ];

    private static List<int> Find(string query, params int[] active) =>
        PlayerNameMatcher.Match(Players, query, active.ToHashSet(), 10).Select(p => p.Id).ToList();

    [TestMethod]
    [DataRow("mcdav", 1)]
    [DataRow("McDavid", 1)]
    [DataRow("connor mcd", 1)]
    [DataRow("stutzle", 2)]
    [DataRow("STÜTZLE", 2)]
    [DataRow("ekman larsson", 3)]
    [DataRow("ekman-larsson", 3)]
    [DataRow("oreilly", 4)]
    [DataRow("o'reilly", 4)]
    [DataRow("jagr", 7)]
    public void Match_ShouldIgnoreCaseAccentsAndPunctuation(string query, int expected) =>
        Find(query).Should().Equal(expected);

    [TestMethod]
    public void Match_ActivePlayers_ShouldComeFirst()
    {
        Find("connor", 6).Should().Equal(6, 5, 1);
    }

    [TestMethod]
    public void Match_ExactLastName_ShouldBeatAPrefix()
    {
        var players = new List<PlayerName>
        {
            new() { Id = 1, FirstName = "Adam", LastName = "Fox" },
            new() { Id = 2, FirstName = "Jake", LastName = "Foxworth" },
        };

        PlayerNameMatcher.Match(players, "fox", new HashSet<int>(), 10).Select(p => p.Id).Should().Equal(1, 2);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("m")]
    [DataRow("zzz")]
    public void Match_TooShortOrNoMatch_ShouldFindNobody(string query) =>
        Find(query).Should().BeEmpty();

    [TestMethod]
    public void Match_ShouldStopAtTheLimit() =>
        PlayerNameMatcher.Match(Players, "connor", new HashSet<int>(), 2).Should().HaveCount(2);

    [TestMethod]
    public async Task FindPlayers_ShouldAddCurrentAndLastTeamAndActiveFlag()
    {
        var repo = new FakeWebPlayerRepository();
        repo.Players.Add(new PlayerProfile { Id = 1, FirstName = "Connor", LastName = "McDavid" });
        repo.Players.Add(new PlayerProfile { Id = 2, FirstName = "Connor", LastName = "Bedard" });
        repo.Active.Add(new ActivePlayer { Id = 2 });
        // Traded since their last game: last played for CHI, now on BOS
        repo.LatestTeams.Add(new PlayerLatestTeam
        {
            PlayerId = 2, TeamId = 16, TeamAbbreviation = "CHI", Position = POSITION.Center,
            CurrentTeamId = 6, CurrentTeamAbbreviation = "BOS",
        });
        var cut = new PlayerGetter(repo);

        var result = await cut.FindPlayers("connor", 2026, 10);

        result.Select(p => p.Id).Should().Equal(2, 1);
        result[0].Name.Should().Be("Connor Bedard");
        result[0].IsActive.Should().BeTrue();
        result[0].Position.Should().Be("C");
        result[0].CurrentTeamId.Should().Be(6);
        result[0].CurrentTeamAbbreviation.Should().Be("BOS");
        result[0].LastTeamAbbreviation.Should().Be("CHI");
        result[1].IsActive.Should().BeFalse();
        result[1].CurrentTeamId.Should().BeNull();
    }
}
