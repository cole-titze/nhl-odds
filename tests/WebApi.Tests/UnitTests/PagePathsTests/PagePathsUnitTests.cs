using FluentAssertions;
using WebApi;

namespace WebApi.Tests.UnitTests.PagePathsTests;

[TestClass]
public class PagePathsUnitTests
{
    [TestMethod]
    [DataRow("Connor McDavid", "connor-mcdavid")]
    [DataRow("Tim Stützle", "tim-stutzle")]
    [DataRow("Ryan O'Reilly", "ryan-oreilly")]
    [DataRow("Lars Eller ", "lars-eller")]
    [DataRow("Frederik Andersen", "frederik-andersen")]
    [DataRow("Mats Zuccarello-Aasen", "mats-zuccarello-aasen")]
    [DataRow("Jesper Bratt Ø", "jesper-bratt-o")]
    [DataRow("St. Louis Blues", "st-louis-blues")]
    public void Slugify_MatchesTheFrontend(string text, string slug)
    {
        PagePaths.Slugify(text).Should().Be(slug);
    }

    [TestMethod]
    public void Game_NamesTheTeams_AwayFirst_ThenTheDay()
    {
        PagePaths.Game(2026020123, "Oilers", "Maple Leafs", new DateTime(2026, 11, 21, 18, 0, 0, DateTimeKind.Utc))
            .Should().Be("/game/2026020123-oilers-at-maple-leafs-2026-11-21");
    }

    [TestMethod]
    public void Game_DayIsCentralTime()
    {
        // 9:30pm Central on the 20th is 3:30am UTC on the 21st
        PagePaths.Game(2026020123, "Oilers", "Kraken", new DateTime(2026, 11, 21, 3, 30, 0, DateTimeKind.Utc))
            .Should().Be("/game/2026020123-oilers-at-kraken-2026-11-20");
    }

    [TestMethod]
    public void Game_TeamTbd_HasOnlyTheDay()
    {
        PagePaths.Game(2026030411, null, "Oilers", new DateTime(2027, 6, 1, 0, 0, 0, DateTimeKind.Utc))
            .Should().Be("/game/2026030411-2027-05-31");
    }
}
