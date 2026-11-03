using Entities.Models.Web;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class GameLineupToGameLineupVmMapperTests
{
    private static GameLineupPlayer P(string side, string group, int line, int slot, string name, string? note = null) =>
        new(side, group, line, slot, name, null, note);

    [TestMethod]
    public void Map_ShouldGroupEachTeamsPlayersIntoLinesInOrder()
    {
        var lineup = new GameLineup
        {
            GameId = 2026020100,
            UpdatedUTC = new DateTime(2026, 11, 3, 20, 0, 0, DateTimeKind.Utc),
            VersionCount = 2,
            StatusReport = "Smith is a game-time decision.",
            Players =
            [
                // Out of order on purpose
                P("Home", "F", 2, 1, "D LW"),
                P("Home", "F", 1, 3, "A RW"),
                P("Home", "F", 1, 1, "A LW"),
                P("Home", "F", 1, 2, "A C"),
                P("Home", "D", 1, 2, "Pair R"),
                P("Home", "D", 1, 1, "Pair L"),
                P("Home", "G", 2, 1, "Backup"),
                P("Home", "G", 1, 1, "Starter"),
                P("Home", "Injured", 1, 1, "Hurt", "upper body"),
                P("Away", "Scratched", 2, 1, "Second"),
                P("Away", "Scratched", 1, 1, "First"),
                P("Away", "Suspended", 1, 1, "Banned"),
            ],
        };

        var vm = GameLineupToGameLineupVmMapper.Map(lineup);

        vm.GameId.Should().Be(2026020100);
        vm.VersionCount.Should().Be(2);
        vm.StatusReport.Should().Be("Smith is a game-time decision.");
        vm.Home.Forwards.Select(l => l.Select(p => p.Name)).Should().BeEquivalentTo(
            new[] { new[] { "A LW", "A C", "A RW" }, new[] { "D LW" } }, o => o.WithStrictOrdering());
        vm.Home.Defense.Single().Select(p => p.Name).Should().Equal("Pair L", "Pair R");
        vm.Home.Goalies.Select(p => p.Name).Should().Equal("Starter", "Backup");
        vm.Home.Injured.Single().Note.Should().Be("upper body");
        vm.Home.Scratched.Should().BeEmpty();
        vm.Away.Forwards.Should().BeEmpty();
        vm.Away.Scratched.Select(p => p.Name).Should().Equal("First", "Second");
        vm.Away.Suspended.Single().Name.Should().Be("Banned");
    }
}
