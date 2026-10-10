using DataGetter.BusinessLogic;
using FluentAssertions;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlArticleTextParserTests
{
    private static string Player(string name, int id) =>
        $"<forge-entity title=\"{name}\" slug=\"{name.ToLowerInvariant().Replace(' ', '-')}-{id}\" code=\"player\">{name}</forge-entity>";

    private static readonly string Props = $"""
        ***NHL.com's fantasy staff provides daily picks.***
        ## **NHL PROPS: SAT. OCT. 10 🎯**

        **Player to watch for point, shots on goal and/or power-play point: {Player("Jackson Blake", 8482809)}, F, CAR (at CHI; 7 p.m. ET; HHN)**

        The Carolina Hurricanes forward plays with {Player("Sebastian Aho", 8478427)}.
        **Others to watch for point:**

        {Player("Nazem Kadri", 8475172)}, F, COL (vs. TOR)
        {Player("Shayne Gostisbehere", 8476906)}, D, CAR (at CHI)
        ## **'NHL GOAL CHASE' PICK 🚨**

        **{Player("Viktor Arvidsson", 8478042)} F, DET (at MTL; 7 p.m. ET; TVAS)**
        ## **'NHL FANTASY STARS' PICK ⭐️**

        {Player("Jaden Schwartz", 8475768)}, F, COL (vs. TOR; 7 p.m. ET; NHLN)
        Schwartz is tied with {Player("Mark Stone", 8475913)} for the lead.
        ## **MORE FANTASY COVERAGE**

        {Player("Connor McDavid", 8478402)}, F, EDM (at SJS)
        """;

    [TestMethod]
    public void ParsePropsPicks_ReadsEachCategory()
    {
        var picks = NhlArticleTextParser.ParsePropsPicks("e1", "h1", Props, new DateTime(2026, 10, 10, 14, 0, 0));

        picks.Select(p => (p.Category, p.PlayerId)).Should().Equal(
            ("Player to watch for point, shots on goal and/or power-play point", 8482809),
            ("Others to watch for point", 8475172),
            ("Others to watch for point", 8476906),
            ("Goal Chase", 8478042),
            ("Fantasy Stars", 8475768));
        var blake = picks[0];
        blake.PickDate.Should().Be(new DateTime(2026, 10, 10));
        blake.Position.Should().Be("F");
        blake.TeamAbbreviation.Should().Be("CAR");
        blake.OpponentAbbreviation.Should().Be("CHI");
        blake.IsHome.Should().BeFalse();
        picks.Single(p => p.PlayerId == 8475172).IsHome.Should().BeTrue();
        picks.Single(p => p.PlayerId == 8476906).Position.Should().Be("D");
    }

    [TestMethod]
    public void ParsePropsPicks_DateRollsOverTheNewYear()
    {
        var body = $"## **NHL PROPS: WED. JAN. 3**\n\n**Player to watch for goal: {Player("Jack Hughes", 8481559)}, F, NJD (vs. NYR)**";

        var picks = NhlArticleTextParser.ParsePropsPicks("e1", "h1", body, new DateTime(2026, 12, 31));

        picks.Single().PickDate.Should().Be(new DateTime(2027, 1, 3));
    }

    [TestMethod]
    public void ParseMentions_CountsEachLinkedPlayer()
    {
        var mentions = NhlArticleTextParser.ParseMentions("e1", "h1", Props + Player("Jackson Blake", 8482809));

        mentions.Should().HaveCount(8);
        var blake = mentions.Single(m => m.NhlId == 8482809);
        blake.Count.Should().Be(2);
        blake.Code.Should().Be("player");
        blake.Title.Should().Be("Jackson Blake");
    }
}