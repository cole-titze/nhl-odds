using Entities.Models.Web;
using Entities.Types;
using Entities.Types.Enums;
using Entities.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WebApi.BusinessLogic.PlayerGetter;
using WebApi.Caching;
using WebApi.Controllers;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.PlayerGetterTests;

[TestClass]
public class PlayerGetterUnitTests
{
    private const int YEAR = 2026;
    private const int TOR = 10;
    private const int BOS = 6;

    private static Game PlayedGame(int id, DateTime date, int homeGoals, int awayGoals,
        PeriodType endPeriod = PeriodType.Regulation, bool played = true) => new()
    {
        Id = id,
        GameDate = date,
        SeasonStartYear = YEAR,
        HomeGoals = homeGoals,
        AwayGoals = awayGoals,
        Winner = homeGoals > awayGoals ? Winner.HOME : Winner.AWAY,
        EndPeriod = endPeriod,
        HasBeenPlayed = played,
        HomeTeam = new Team { Id = TOR, LocationName = "Toronto", TeamName = "Maple Leafs" },
        AwayTeam = new Team { Id = BOS, LocationName = "Boston", TeamName = "Bruins" },
    };

    private static FakeWebPlayerRepository RepoWithMatthews()
    {
        var repo = new FakeWebPlayerRepository();
        repo.Players.Add(new PlayerProfile
        {
            Id = 8479318,
            FirstName = "Auston",
            LastName = "Matthews",
            Position = POSITION.Center,
            SweaterNumber = 34,
            BirthDate = new DateTime(1997, 9, 17),
            CurrentTeam = new Team { Id = TOR, LocationName = "Toronto", TeamName = "Maple Leafs" },
        });
        // Three home games, out of order: a win, an overtime loss and a regulation loss
        repo.Lines.Add(new PlayerGameLine
        {
            PlayerId = 8479318, TeamId = TOR, Position = POSITION.Center,
            Game = PlayedGame(2026020010, new DateTime(2026, 10, 10, 23, 0, 0, DateTimeKind.Utc), 4, 1),
            Goals = 2, Assists = 1, ShotsOnGoal = 6, PlusMinus = 2, TimeOnIceSeconds = 1200,
        });
        repo.Lines.Add(new PlayerGameLine
        {
            PlayerId = 8479318, TeamId = TOR, Position = POSITION.Center,
            Game = PlayedGame(2026020020, new DateTime(2026, 10, 20, 23, 0, 0, DateTimeKind.Utc), 2, 3, PeriodType.Overtime),
            Goals = 0, Assists = 1, ShotsOnGoal = 3, PlusMinus = -1, TimeOnIceSeconds = 1300,
        });
        repo.Lines.Add(new PlayerGameLine
        {
            PlayerId = 8479318, TeamId = TOR, Position = POSITION.Center,
            Game = PlayedGame(2026020015, new DateTime(2026, 10, 15, 23, 0, 0, DateTimeKind.Utc), 1, 5),
            ShotsOnGoal = 2, TimeOnIceSeconds = 1100,
        });
        return repo;
    }

    [TestMethod]
    public async Task GetPlayer_UnknownPlayer_ReturnsNull()
    {
        var cut = new PlayerGetter(new FakeWebPlayerRepository());

        var result = await cut.GetPlayer(1, YEAR);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetPlayer_KnownPlayer_MapsProfileAndCurrentTeam()
    {
        var cut = new PlayerGetter(RepoWithMatthews());

        var result = await cut.GetPlayer(8479318, YEAR);

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Auston");
        result.Position.Should().Be("C");
        result.SweaterNumber.Should().Be((short)34);
        result.BirthDate.Should().Be("1997-09-17");
        result.CurrentTeam!.Id.Should().Be(TOR);
        result.SeasonStartYear.Should().Be(YEAR);
    }

    [TestMethod]
    public async Task GetPlayer_Games_AreNewestFirstWithResults()
    {
        var cut = new PlayerGetter(RepoWithMatthews());

        var result = await cut.GetPlayer(8479318, YEAR);

        var games = result!.Games.ToList();
        games.Select(g => g.GameId).Should().Equal(2026020020, 2026020015, 2026020010);
        games.Select(g => g.Result).Should().Equal("OTL", "L", "W");
        games[2].IsHome.Should().BeTrue();
        games[2].Opponent.Id.Should().Be(BOS);
        games[2].TeamGoals.Should().Be(4);
        games[2].OpponentGoals.Should().Be(1);
        games[2].Stats.Points.Should().Be(3);
    }

    [TestMethod]
    public async Task GetPlayer_SeasonTotals_SumTheGames()
    {
        var cut = new PlayerGetter(RepoWithMatthews());

        var result = await cut.GetPlayer(8479318, YEAR);

        var totals = result!.SeasonTotals;
        totals.GamesPlayed.Should().Be(3);
        totals.Goals.Should().Be(2);
        totals.Assists.Should().Be(2);
        totals.Points.Should().Be(4);
        totals.ShotsOnGoal.Should().Be(11);
        totals.PlusMinus.Should().Be(1);
        totals.GoalsAgainstAverage.Should().BeNull();
    }

    [TestMethod]
    public async Task GetPlayer_UnplayedGames_AreLeftOut()
    {
        var repo = RepoWithMatthews();
        repo.Lines.Add(new PlayerGameLine
        {
            PlayerId = 8479318, TeamId = TOR, Position = POSITION.Center,
            Game = PlayedGame(2026020030, new DateTime(2026, 10, 30, 23, 0, 0, DateTimeKind.Utc), 0, 0, played: false),
        });
        var cut = new PlayerGetter(repo);

        var result = await cut.GetPlayer(8479318, YEAR);

        result!.Games.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task GetPlayer_NoGamesThisSeason_ReturnsProfileWithEmptyGames()
    {
        var cut = new PlayerGetter(RepoWithMatthews());

        var result = await cut.GetPlayer(8479318, YEAR - 5);

        result.Should().NotBeNull();
        result!.Games.Should().BeEmpty();
        result.SeasonTotals.GamesPlayed.Should().Be(0);
    }

    [TestMethod]
    public async Task GetPlayer_Goalie_GetsSavePctAndGoalsAgainstAverage()
    {
        var repo = new FakeWebPlayerRepository();
        repo.Players.Add(new PlayerProfile { Id = 1, Position = POSITION.Goalie });
        repo.Lines.Add(new PlayerGameLine
        {
            PlayerId = 1, TeamId = TOR, Position = POSITION.Goalie,
            Game = PlayedGame(2026020010, new DateTime(2026, 10, 10, 23, 0, 0, DateTimeKind.Utc), 4, 1),
            Saves = 29, GoalsAgainst = 1, ShotsAgainst = 30, TimeOnIceSeconds = 3600, IsStarter = true,
        });
        repo.Lines.Add(new PlayerGameLine
        {
            PlayerId = 1, TeamId = TOR, Position = POSITION.Goalie,
            Game = PlayedGame(2026020015, new DateTime(2026, 10, 15, 23, 0, 0, DateTimeKind.Utc), 1, 5),
            Saves = 15, GoalsAgainst = 5, ShotsAgainst = 20, TimeOnIceSeconds = 1800,
        });
        var cut = new PlayerGetter(repo);

        var result = await cut.GetPlayer(1, YEAR);

        result!.Position.Should().Be("G");
        result.Games.First().Stats.SavePct.Should().BeApproximately(0.75, 1e-9);
        result.SeasonTotals.SavePct.Should().BeApproximately(44.0 / 50, 1e-9);
        result.SeasonTotals.GoalsAgainstAverage.Should().BeApproximately(6 * 3600.0 / 5400, 1e-9);
    }

    [TestMethod]
    public async Task GetTeamRoster_ListsForwardsThenDefenseThenGoaliesByNumber()
    {
        var repo = new FakeWebPlayerRepository();
        repo.Roster.AddRange(new[]
        {
            new RosterPlayer { Id = 1, LastName = "Goalie", TeamId = TOR, Position = POSITION.Goalie, SweaterNumber = 35 },
            new RosterPlayer { Id = 2, LastName = "Dman", TeamId = TOR, Position = POSITION.Defenseman, SweaterNumber = 44 },
            new RosterPlayer { Id = 3, LastName = "Wing", TeamId = TOR, Position = POSITION.LeftWing, SweaterNumber = 88 },
            new RosterPlayer { Id = 4, LastName = "Center", TeamId = TOR, Position = POSITION.Center, SweaterNumber = 34 },
            new RosterPlayer { Id = 5, LastName = "NoNumber", TeamId = TOR, Position = POSITION.RightWing },
            new RosterPlayer { Id = 6, LastName = "Other", TeamId = BOS, Position = POSITION.Center, SweaterNumber = 37 },
        });
        var cut = new PlayerGetter(repo);

        var result = await cut.GetTeamRoster(TOR, YEAR);

        result.Select(p => p.Id).Should().Equal(4, 3, 5, 2, 1);
        result.Select(p => p.Position).Should().Equal("C", "L", "R", "D", "G");
    }

    [TestMethod]
    public async Task GetGamePlayers_UnknownGame_ReturnsNull()
    {
        var cut = new PlayerGetter(new FakeWebPlayerRepository());

        var result = await cut.GetGamePlayers(2026020001);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetGamePlayers_PlayedGame_SplitsBoxScoreByTeam()
    {
        var game = PlayedGame(2026020010, new DateTime(2026, 10, 10, 23, 0, 0, DateTimeKind.Utc), 4, 1);
        var repo = new FakeWebPlayerRepository();
        repo.Games.Add(new GamePlayers
        {
            Game = game,
            Lines =
            {
                new PlayerGameLine { PlayerId = 1, TeamId = TOR, Position = POSITION.Goalie, Game = game, Saves = 20, ShotsAgainst = 21, GoalsAgainst = 1 },
                new PlayerGameLine { PlayerId = 2, TeamId = TOR, Position = POSITION.Center, Game = game, Goals = 2 },
                new PlayerGameLine { PlayerId = 3, TeamId = BOS, Position = POSITION.Defenseman, Game = game, Assists = 1 },
            },
        });
        var cut = new PlayerGetter(repo);

        var result = await cut.GetGamePlayers(2026020010);

        result!.HasStats.Should().BeTrue();
        result.Home.Select(p => p.Id).Should().Equal(2, 1);
        result.Home.First().Stats!.Goals.Should().Be(2);
        result.Away.Select(p => p.Id).Should().Equal(3);
        result.Away.First().Stats!.Points.Should().Be(1);
    }

    [TestMethod]
    public async Task GetGamePlayers_UpcomingGame_ListsRostersWithoutStats()
    {
        var game = PlayedGame(2026020030, new DateTime(2026, 10, 30, 23, 0, 0, DateTimeKind.Utc), 0, 0, played: false);
        var repo = new FakeWebPlayerRepository();
        repo.Games.Add(new GamePlayers
        {
            Game = game,
            Roster =
            {
                new RosterPlayer { Id = 1, TeamId = TOR, Position = POSITION.Center },
                new RosterPlayer { Id = 2, TeamId = BOS, Position = POSITION.Goalie },
            },
        });
        var cut = new PlayerGetter(repo);

        var result = await cut.GetGamePlayers(2026020030);

        result!.HasStats.Should().BeFalse();
        result.Home.Single().Stats.Should().BeNull();
        result.Away.Single().Id.Should().Be(2);
    }

    [TestMethod]
    public async Task GetSitemapPlayers_MapsLastGameDate_SortedById()
    {
        var repo = new FakeWebPlayerRepository();
        var last = new DateTime(2026, 11, 5, 23, 0, 0, DateTimeKind.Utc);
        var retired = new DateTime(2012, 4, 7, 23, 0, 0, DateTimeKind.Utc);
        repo.LastGames.Add(new ActivePlayer { Id = 8479318, LastGameDateUTC = last });
        repo.LastGames.Add(new ActivePlayer { Id = 8448208, LastGameDateUTC = retired });
        var cut = new PlayerGetter(repo);

        var result = await cut.GetSitemapPlayers(new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc));

        result.Select(p => p.Id).Should().Equal(8448208, 8479318);
        result[0].LastGameDate.Should().Be(retired);
        result[1].LastGameDate.Should().Be(last);
    }

    [TestMethod]
    public async Task GetSitemapPlayers_LeavesOutFutureDates()
    {
        var repo = new FakeWebPlayerRepository();
        var now = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);
        repo.LastGames.Add(new ActivePlayer { Id = 1, LastGameDateUTC = now.AddDays(2) });
        repo.LastGames.Add(new ActivePlayer { Id = 2, LastGameDateUTC = now.AddDays(-1) });
        var cut = new PlayerGetter(repo);

        var result = await cut.GetSitemapPlayers(now);

        result.Should().ContainSingle().Which.Id.Should().Be(2);
    }

    [TestMethod]
    public async Task Controller_GetSitemapPlayers_ReturnsEveryPlayerWithAGame()
    {
        var repo = new FakeWebPlayerRepository();
        repo.LastGames.Add(new ActivePlayer { Id = 8479318, LastGameDateUTC = new DateTime(2026, 11, 5, 23, 0, 0, DateTimeKind.Utc) });
        var controller = new PlayerController(new PlayerGetter(repo), ApiCache.Create());

        var result = await controller.GetSitemapPlayers();

        result.Should().BeOfType<Ok<List<ActivePlayerVM>>>()
            .Which.Value.Should().ContainSingle().Which.Id.Should().Be(8479318);
    }

    [TestMethod]
    public async Task Controller_UnknownPlayer_Returns404()
    {
        var controller = new PlayerController(new PlayerGetter(new FakeWebPlayerRepository()), ApiCache.Create());

        var result = await controller.GetPlayer(1, YEAR);

        result.Should().BeOfType<NotFound>();
    }

    [TestMethod]
    public async Task Controller_UnknownGame_Returns404()
    {
        var controller = new PlayerController(new PlayerGetter(new FakeWebPlayerRepository()), ApiCache.Create());

        var result = await controller.GetGamePlayers(2026020001);

        result.Should().BeOfType<NotFound>();
    }
}
