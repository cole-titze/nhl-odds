using Entities.Types;
using Entities.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Caching;
using WebApi.Calendar;
using WebApi.Controllers;

namespace WebApi.Tests.BusinessLogic.UnitTests.CalendarTests;

[TestClass]
public class TeamCalendarTests
{
    private const string SITE = "https://nhl.example.com";
    private static readonly DateTime NOW = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    private static MatchupTeamVM Leafs(double? odds, int goals = 0) => new()
    {
        Id = 10, LocationName = "Toronto", TeamName = "Maple Leafs", ModelOdds = odds, Goals = goals, Team = Winner.HOME,
    };

    private static MatchupTeamVM Bruins(double? odds, int goals = 0) => new()
    {
        Id = 6, LocationName = "Boston", TeamName = "Bruins", ModelOdds = odds, Goals = goals, Team = Winner.AWAY,
    };

    private static TeamVM Team(params GameOddsVM[] games) => new()
    {
        Id = 10, LocationName = "Toronto", TeamName = "Maple Leafs", GameOddsVM = games,
    };

    private static readonly GameOddsVM Upcoming = new()
    {
        Id = 2026020150,
        GameDate = new DateTime(2026, 11, 5, 0, 0, 0),
        HomeTeam = Leafs(0.58),
        AwayTeam = Bruins(0.42),
    };

    private static readonly GameOddsVM Played = new()
    {
        Id = 2026020100,
        GameDate = new DateTime(2026, 10, 31, 23, 0, 0),
        HomeTeam = Leafs(0.58, 3),
        AwayTeam = Bruins(0.42, 4),
        Winner = Winner.AWAY,
        HasBeenPlayed = true,
    };

    // Unfolds the feed (RFC 5545 3.1) and returns its lines
    private static string[] Lines(string ics) =>
        ics.Replace("\r\n ", "").Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    [TestMethod]
    public void Build_ShouldWrapEventsInACalendar()
    {
        var lines = Lines(TeamCalendarBuilder.Build(Team(Upcoming, Played), 2026, SITE, NOW));

        lines.First().Should().Be("BEGIN:VCALENDAR");
        lines.Last().Should().Be("END:VCALENDAR");
        lines.Should().Contain("VERSION:2.0");
        lines.Should().Contain("X-WR-CALNAME:Toronto Maple Leafs · NHL Odds");
        lines.Count(l => l == "BEGIN:VEVENT").Should().Be(2);
    }

    [TestMethod]
    public void Build_ShouldListGamesOldestFirstWithStableIds()
    {
        var lines = Lines(TeamCalendarBuilder.Build(Team(Upcoming, Played), 2026, SITE, NOW));

        lines.Where(l => l.StartsWith("UID:")).Should().Equal(
            "UID:game-2026020100@nhl.example.com",
            "UID:game-2026020150@nhl.example.com");
    }

    [TestMethod]
    public void Build_ShouldWriteTimesInUtc()
    {
        var lines = Lines(TeamCalendarBuilder.Build(Team(Upcoming), 2026, SITE, NOW));

        lines.Should().Contain("DTSTART:20261105T000000Z");
        lines.Should().Contain("DTEND:20261105T023000Z");
        lines.Should().Contain("DTSTAMP:20261103T120000Z");
    }

    [TestMethod]
    public void Build_UpcomingGame_ShouldShowTheTeamsModelOdds()
    {
        var lines = Lines(TeamCalendarBuilder.Build(Team(Upcoming), 2026, SITE, NOW));

        lines.Should().Contain("SUMMARY:Bruins at Maple Leafs (Maple Leafs 58%)");
        lines.Should().Contain("LOCATION:Toronto");
        lines.Should().Contain("URL:https://nhl.example.com/game/2026020150");
        lines.Single(l => l.StartsWith("DESCRIPTION:")).Should().Be(
            "DESCRIPTION:The NHL Odds model gives the Bruins 42% and the Maple Leafs 58%.\\n" +
            "Odds and analysis: https://nhl.example.com/game/2026020150");
    }

    [TestMethod]
    public void Build_AwayTeamFeed_ShouldShowItsOwnOdds()
    {
        var bruins = new TeamVM { Id = 6, LocationName = "Boston", TeamName = "Bruins", GameOddsVM = [Upcoming] };

        var lines = Lines(TeamCalendarBuilder.Build(bruins, 2026, SITE, NOW));

        lines.Should().Contain("SUMMARY:Bruins at Maple Leafs (Bruins 42%)");
    }

    [TestMethod]
    public void Build_UpcomingGameWithoutPrediction_ShouldSaySoAndLeaveOddsOut()
    {
        var game = new GameOddsVM { Id = 1, GameDate = Upcoming.GameDate, HomeTeam = Leafs(null), AwayTeam = Bruins(null) };

        var lines = Lines(TeamCalendarBuilder.Build(Team(game), 2026, SITE, NOW));

        lines.Should().Contain("SUMMARY:Bruins at Maple Leafs");
        lines.Single(l => l.StartsWith("DESCRIPTION:")).Should().StartWith("DESCRIPTION:Model odds are posted closer to the game.");
    }

    [TestMethod]
    public void Build_PlayedGame_ShouldShowTheFinalScore()
    {
        var lines = Lines(TeamCalendarBuilder.Build(Team(Played), 2026, SITE, NOW));

        lines.Should().Contain("SUMMARY:Bruins 4\\, Maple Leafs 3 (Final)");
        lines.Single(l => l.StartsWith("DESCRIPTION:")).Should().Contain(
            "gave the Bruins 42% and the Maple Leafs 58%.\\nIt picked the loser.");
    }

    [TestMethod]
    public void Build_MissingTeam_ShouldSayTbd()
    {
        var game = new GameOddsVM { Id = 1, GameDate = Upcoming.GameDate, HomeTeam = Leafs(null) };

        var lines = Lines(TeamCalendarBuilder.Build(Team(game), 2026, SITE, NOW));

        lines.Should().Contain("SUMMARY:TBD at Maple Leafs");
    }

    [TestMethod]
    public void Build_ShouldFoldLinesAt75Octets()
    {
        var ics = TeamCalendarBuilder.Build(Team(Upcoming, Played), 2026, SITE, NOW);

        ics.Should().EndWith("\r\n");
        foreach (var line in ics.Split("\r\n"))
            System.Text.Encoding.UTF8.GetByteCount(line).Should().BeLessThanOrEqualTo(75);
        ics.Split("\r\n").Should().Contain(l => l.StartsWith(' '), "the descriptions are long enough to fold");
    }

    [TestMethod]
    public void Fold_ShouldNotSplitMultiByteCharacters()
    {
        var team = new TeamVM { Id = 1, LocationName = "Montréal", TeamName = new string('é', 60), GameOddsVM = [] };

        var ics = TeamCalendarBuilder.Build(team, 2026, SITE, NOW);

        // Splitting inside a character would leave replacement characters after a round trip
        var roundTrip = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(ics));
        roundTrip.Should().Be(ics);
        Lines(ics).Should().Contain($"X-WR-CALNAME:Montréal {new string('é', 60)} · NHL Odds");
    }

    [TestMethod]
    public void Escape_ShouldEscapeTextSpecialCharacters()
    {
        TeamCalendarBuilder.Escape("a\\b;c,d\ne").Should().Be(@"a\\b\;c\,d\ne");
    }

    [TestMethod]
    public void Seasons_ShouldStartOnSeptember20()
    {
        Seasons.Current(new DateTime(2026, 9, 19)).Should().Be(2025);
        Seasons.Current(new DateTime(2026, 9, 20)).Should().Be(2026);
    }

    private static (CalendarController controller, ITeamGetter getter) Controller(Dictionary<string, string?>? config = null)
    {
        var getter = A.Fake<ITeamGetter>();
        A.CallTo(() => getter.GetAllTeamsStats(A<int>._)).ReturnsLazily(() => new TeamsVM { Teams = [Team(Upcoming, Played)] });
        var services = new ServiceCollection()
            .AddSingleton(getter)
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(config ?? []).Build())
            .AddKeyedSingleton(ApiCache.ServiceKey, (_, _) => ApiCache.Create())
            .BuildServiceProvider();
        return (ActivatorUtilities.CreateInstance<CalendarController>(services), getter);
    }

    private static async Task<HttpContext> Execute(IResult result)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        context.Response.Body = new MemoryStream();
        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        return context;
    }

    [TestMethod]
    public async Task Team_ShouldServeTheFeedFromTheAllTeamsCache()
    {
        var (cut, getter) = Controller();

        await cut.Team(10);
        var context = await Execute(await cut.Team(10));

        A.CallTo(() => getter.GetAllTeamsStats(A<int>._)).MustHaveHappenedOnceExactly();
        context.Response.ContentType.Should().Be("text/calendar; charset=utf-8");
        context.Response.Headers.CacheControl.ToString().Should().Be("public, max-age=3600");
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().StartWith("BEGIN:VCALENDAR\r\n");
        body.Should().Contain("URL:https://nhl.half-built.com/game/2026020150");
    }

    [TestMethod]
    public async Task Team_ShouldLinkToSiteUrlWhenSet()
    {
        var (cut, _) = Controller(new() { ["SITE_URL"] = "https://staging.example.com/" });

        var context = await Execute(await cut.Team(10));

        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("URL:https://staging.example.com/game/2026020150");
    }

    [TestMethod]
    public async Task Team_UnknownTeam_ShouldBeNotFound()
    {
        var (cut, _) = Controller();

        var context = await Execute(await cut.Team(99));

        context.Response.StatusCode.Should().Be(404);
    }
}
