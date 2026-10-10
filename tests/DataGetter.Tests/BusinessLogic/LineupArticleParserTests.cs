using DataGetter.BusinessLogic;
using Entities.DbModels;
using FluentAssertions;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class LineupArticleParserTests
{
    // Formatting quirks seen in the real article: a "## " start-time line, "projected lines", "--Name" and "–"
    // separators, non-breaking spaces, "***Scratched:*** *A*" vs "***Scratched:** A*", and ";" in the injured list
    private const string ARTICLE = """
        ## **[FANTASY COVERAGE](https://www.nhl.com/fantasy/) 📈**
        ## **CANUCKS (2-3-0) at DEVILS (1-2-0)**

        ## **3:30 p.m. ET; MSGSN, SNP**

        **Canucks projected lines**

        Jake DeBrusk -- Elias Pettersson -- Jonathan Lekkerimaki

        Paul Cotter -- Marco Rossi -- Brock Boeser

        Liam Ohgren -- Max Sasson -- Linus Karlsson

        Arshdeep Bains -- Drew O’Connor -- Aatu Raty

        Zeev Buium – Filip Hronek

        Jamie Oleksiak --Tom Willander

        Elias Pettersson -- Victor Mancini

        Leevi Merilainen

        Kevin Lankinen

        ***Scratched:*** *Brendan Gallagher*

        ***Injured:** Thatcher Demko (hip), Filip Chytil (shoulder); Nick Bjugstad (upper body)*

        **Devils projected lineup**

        Timo Meier -- Nico Hischier -- Luke Evangelista

        Jesper Bratt -- Jack Hughes -- Anthony Mantha

        Arseny Gritsyuk -- Evan Rodrigues -- Dawson Mercer

        Amadeus Lombardi -- Cody Glass

        Luke Hughes -- Brett Pesce

        Jonas Siegenthaler -- Dougie Hamilton

        Brenden Dillon -- Declan Chisholm

        Johnathan Kovacevic

        Jake Allen

        Nico Daws

        ***Scratched:** None*

        ***Injured:** None*

        ***Suspended:** Stefan Noesen*

        **Status report**

        The Canucks did not practice Friday. ... Demko will not play.
        """;

    private static char? Positions(string team, string name) => (team, name) switch
    {
        ("DEVILS", "Johnathan Kovacevic") => 'D',
        ("DEVILS", "Amadeus Lombardi") => 'F',
        ("DEVILS", "Cody Glass") => 'F',
        _ => null,
    };

    [TestMethod]
    public void Parse_ReadsEachTeamsLinesListsAndStatusReport()
    {
        var sections = LineupArticleParser.Parse(ARTICLE, Positions);

        var section = sections.Should().ContainSingle().Subject;
        section.AwayName.Should().Be("CANUCKS");
        section.HomeName.Should().Be("DEVILS");
        section.StatusReport.Should().StartWith("The Canucks did not practice Friday.");

        var canucks = section.Players.Where(p => p.Side == "Away").ToList();
        canucks.Count(p => p.Group == "F").Should().Be(12);
        canucks.Count(p => p.Group == "D").Should().Be(6);
        canucks.Where(p => p.Group == "G").Select(p => p.Name).Should().Equal("Leevi Merilainen", "Kevin Lankinen");
        canucks.Should().Contain(new LineupArticleParser.ParsedPlayer("Away", "F", 4, 2, "Drew O’Connor", null));
        canucks.Should().Contain(new LineupArticleParser.ParsedPlayer("Away", "D", 2, 2, "Tom Willander", null));
        canucks.Should().Contain(new LineupArticleParser.ParsedPlayer("Away", "Scratched", 1, 1, "Brendan Gallagher", null));
        canucks.Where(p => p.Group == "Injured").Select(p => (p.Name, p.Note)).Should().Equal(
            ("Thatcher Demko", "hip"), ("Filip Chytil", "shoulder"), ("Nick Bjugstad", "upper body"));
    }

    [TestMethod]
    public void Parse_ElevenForwardsSevenDefensemen()
    {
        var devils = LineupArticleParser.Parse(ARTICLE, Positions).Single().Players.Where(p => p.Side == "Home").ToList();

        // A 2-name fourth line is still forwards, and a defenseman listed alone isn't a goalie
        devils.Count(p => p.Group == "F").Should().Be(11);
        devils.Should().Contain(new LineupArticleParser.ParsedPlayer("Home", "F", 4, 2, "Cody Glass", null));
        devils.Count(p => p.Group == "D").Should().Be(7);
        devils.Should().Contain(new LineupArticleParser.ParsedPlayer("Home", "D", 4, 1, "Johnathan Kovacevic", null));
        devils.Where(p => p.Group == "G").Select(p => p.Name).Should().Equal("Jake Allen", "Nico Daws");
        devils.Should().NotContain(p => p.Group == "Scratched" || p.Group == "Injured");
        devils.Should().Contain(new LineupArticleParser.ParsedPlayer("Home", "Suspended", 1, 1, "Stefan Noesen", null));
    }

    [TestMethod]
    public void Parse_SectionHashChangesOnlyWithItsOwnText()
    {
        var other = "\n## **FLYERS (0-3-2) at BRUINS (3-2-0)**\n\n**Flyers projected lineup**\n\nDan Vladar\n";
        var first = LineupArticleParser.Parse(ARTICLE);
        var withAnotherGame = LineupArticleParser.Parse(ARTICLE + other);

        withAnotherGame.Should().HaveCount(2);
        withAnotherGame[0].SectionHash.Should().Be(first[0].SectionHash);
    }

    [TestMethod]
    public void MatchPlayer_UsesPositionToTellSameNamesApart()
    {
        List<NhlLineupParseManager.RosterPlayer> roster =
        [
            new(8480012, NhlLineupParseManager.NormalizeName("Elias Pettersson"), 'F'),
            new(8483678, NhlLineupParseManager.NormalizeName("Elias Pettersson"), 'D'),
            new(8482809, NhlLineupParseManager.NormalizeName("Jackson Blake"), 'F'),
        ];

        NhlLineupParseManager.MatchPlayer(roster, "Elias Pettersson", "F").Should().Be(8480012);
        NhlLineupParseManager.MatchPlayer(roster, "Elias Pettersson", "D").Should().Be(8483678);
        // On a list there's no position to go by, so no guess
        NhlLineupParseManager.MatchPlayer(roster, "Elias Pettersson", "Scratched").Should().BeNull();
        NhlLineupParseManager.MatchPlayer(roster, "JACKSON  BLAKE", "Injured").Should().Be(8482809);
    }

    [TestMethod]
    public void NormalizeName_IgnoresAccentsCaseAndPunctuation()
    {
        NhlLineupParseManager.NormalizeName("Ryan O’Reilly").Should().Be(NhlLineupParseManager.NormalizeName("Ryan O'Reilly"));
        NhlLineupParseManager.NormalizeName("J.J. Peterka").Should().Be("jjpeterka");
        NhlLineupParseManager.NormalizeName("Montréal Canadiens").Should().Be("montrealcanadiens");
    }

    [TestMethod]
    public void TeamIdOf_MatchesTheEndOfTheFullName()
    {
        List<DbSeasonTeam> teams =
        [
            new() { TeamId = 17, Name = "Detroit Red Wings" },
            new() { TeamId = 8, Name = "Montréal Canadiens" },
            new() { TeamId = 26, Name = "Los Angeles Kings" },
        ];

        NhlLineupParseManager.TeamIdOf("RED WINGS", teams).Should().Be(17);
        NhlLineupParseManager.TeamIdOf("CANADIENS", teams).Should().Be(8);
        NhlLineupParseManager.TeamIdOf("KINGS", teams).Should().Be(26);
        NhlLineupParseManager.TeamIdOf("FLYERS", teams).Should().BeNull();
    }

    [TestMethod]
    public void MatchAnyPlayer_OnlyUniqueNames()
    {
        var players = new Dictionary<string, List<int>>
        {
            [NhlLineupParseManager.NormalizeName("Mathew Barzal")] = [8478445],
            [NhlLineupParseManager.NormalizeName("Sebastian Aho")] = [8478427, 8480222],
        };

        NhlLineupParseManager.MatchAnyPlayer(players, "Mathew Barzal").Should().Be(8478445);
        NhlLineupParseManager.MatchAnyPlayer(players, "Sebastian Aho").Should().BeNull();
        NhlLineupParseManager.MatchAnyPlayer(players, "Nobody Here").Should().BeNull();
    }
}