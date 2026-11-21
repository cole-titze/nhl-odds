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
    public void Game_NamesTheTeams_AwayFirst()
    {
        PagePaths.Game(2026020123, "Oilers", "Maple Leafs").Should().Be("/game/2026020123-oilers-at-maple-leafs");
    }

    [TestMethod]
    public void Game_TeamTbd_IsIdOnly()
    {
        PagePaths.Game(2026030411, null, "Oilers").Should().Be("/game/2026030411");
    }
}
