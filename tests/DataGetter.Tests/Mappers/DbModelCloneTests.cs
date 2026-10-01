using Entities.DbModels;
using Entities.DbModels.GamePlayEvents;
using Entities.Types;
using FluentAssertions;

namespace DataGetter.Tests.Mappers;

/// <summary>
/// Clone() updates a tracked EF entity in place. It must copy values only: copying a navigation
/// property from a freshly mapped (unattached) object sets it to null, which EF treats as severing
/// a required relationship and deletes the row on SaveChanges. That silently dropped most player
/// stats for games first saved from a pre-game roster snapshot.
/// </summary>
[TestClass]
public class DbModelCloneTests
{
    private static readonly DbGameRaw Game = new() { Id = 1 };
    private static readonly DbTeam Team = new() { Id = 10 };
    private static readonly DbPlayer Player = new() { Id = 100 };

    [TestMethod]
    public void SkaterStatsClone_KeepsNavigations_AndCopiesValues()
    {
        var tracked = new DbGameSkaterStats
        {
            GameId = 1,
            PlayerId = 100,
            TeamId = 10,
            Game = Game,
            Team = Team,
            Player = Player,
        };
        var final = new DbGameSkaterStats
        {
            GameId = 1,
            PlayerId = 100,
            TeamId = 11,
            TimeOnIceSeconds = 1213,
            ShotsOnGoal = 2,
            Position = POSITION.Defenseman,
        };

        tracked.Clone(final);

        tracked.Game.Should().BeSameAs(Game);
        tracked.Team.Should().BeSameAs(Team);
        tracked.Player.Should().BeSameAs(Player);
        tracked.IsEquivalentTo(final).Should().BeTrue();
    }

    [TestMethod]
    public void GoalieStatsClone_KeepsNavigations_AndCopiesValues()
    {
        var tracked = new DbGameGoalieStats
        {
            GameId = 1,
            PlayerId = 100,
            TeamId = 10,
            Game = Game,
            Team = Team,
            Player = Player,
        };
        var final = new DbGameGoalieStats { GameId = 1, PlayerId = 100, TeamId = 10, IsStarter = true };

        tracked.Clone(final);

        tracked.Game.Should().BeSameAs(Game);
        tracked.Team.Should().BeSameAs(Team);
        tracked.Player.Should().BeSameAs(Player);
        tracked.IsStarter.Should().BeTrue();
    }

    [TestMethod]
    public void PlayerClone_KeepsTeamNavigation()
    {
        var tracked = new DbPlayer { Id = 100, CurrentTeamId = 10, Team = Team };

        tracked.Clone(new DbPlayer { Id = 100, CurrentTeamId = 10, FirstName = "Updated" });

        tracked.Team.Should().BeSameAs(Team);
        tracked.FirstName.Should().Be("Updated");
    }

    [TestMethod]
    public void PeriodEndClone_AcceptsPeriodEnd()
    {
        var tracked = new DbPeriodEnd { Id = 5, GameId = 1, PeriodNumber = 1 };

        tracked.Clone(new DbPeriodEnd { Id = 5, GameId = 1, PeriodNumber = 2 });

        tracked.PeriodNumber.Should().Be(2);
    }

    [TestMethod]
    public void ShootoutCompleteClone_AcceptsShootoutComplete()
    {
        var tracked = new DbShootoutComplete { Id = 5, GameId = 1, SortOrder = 1 };

        tracked.Clone(new DbShootoutComplete { Id = 5, GameId = 1, SortOrder = 2 });

        tracked.SortOrder.Should().Be(2);
    }
}