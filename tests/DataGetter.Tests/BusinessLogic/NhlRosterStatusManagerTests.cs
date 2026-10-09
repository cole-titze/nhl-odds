using DatabaseAccess.RosterStatusRepository;
using DataGetter.BusinessLogic;
using Entities.ServiceModels;
using FluentAssertions;
using Services.NhlData;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlRosterStatusManagerTests
{
    private const string REPORT = """

        Playing Roster - MONTRÉAL CANADIENS
        Season: 2026-27
        Last update: 15:36:05 10/08/2026


        Active

        SW#       Name                     Pos       Height    Weight         Born           Birthplace
        1         ONE PLAYER               G         6'2"      190lbs.        2000-01-01     Brno, CZE
        2         TWO PLAYER               D         6'2"      212lbs.        2000-01-02     Eden Prairie MN, USA
        3         TWO PLAYER               D         6'2"      212lbs.        2000-01-02     Eden Prairie MN, USA
        18        ANTHONY-JOHN (AJ) GREER  L         6'3"      224lbs.        1996-12-14     Joliette QC, CAN
        19        JOSÉ ÅBERG               C         6'0"      190lbs.        2000-01-03     Ylojarvi, FIN
        20        FIVE PLAYER              R         6'0"      190lbs.        2000-01-05     Oulu, FIN

        Injury Reserve List -
        3         IAN MOORE                D         6'3"      209lbs.        2002-01-04     Salt Lake City UT, USA   2026-09-28
                  RYAN ELLIS               D         5'10"     180lbs.        1991-01-03     Hamilton ON, CAN         2021-11-16

        Active - 5
        On IRL - 2
        Total - 7

        Players by Country:
        USA-3
        """;

    [TestMethod]
    public void Parse_ReadsActiveAndInjuredReserveRows()
    {
        var entries = RosterReportParser.Parse(REPORT);

        entries.Should().HaveCount(7, "the repeated TWO PLAYER row is dropped");
        entries.Should().OnlyContain(e => e.TeamName == "MONTRÉAL CANADIENS" && e.ReportUpdated == new DateTime(2026, 10, 8, 15, 36, 5));
        entries.Where(e => !e.IsInjuredReserve).Select(e => e.Name).Should()
            .Equal("ONE PLAYER", "TWO PLAYER", "ANTHONY-JOHN (AJ) GREER", "JOSÉ ÅBERG", "FIVE PLAYER");

        var moore = entries.Single(e => e.Name == "IAN MOORE");
        moore.IsInjuredReserve.Should().BeTrue();
        moore.Position.Should().Be('D');
        moore.BirthDate.Should().Be(new DateTime(2002, 1, 4));
        moore.InjuredReserveDate.Should().Be(new DateTime(2026, 9, 28));

        // Long-term IR rows have no sweater number
        entries.Single(e => e.Name == "RYAN ELLIS").InjuredReserveDate.Should().Be(new DateTime(2021, 11, 16));
    }

    [TestMethod]
    public void Parse_ReadsInjuredReserveDateRunningIntoLongBirthplace()
    {
        const string report = """
            Playing Roster - EDMONTON OILERS

            Injury Reserve List -
            34        COLTON DACH              C         6'3"      215lbs.        2003-01-04     Fort Saskatchewan AB, CAN2026-10-07

            Active - 0
            """;

        var dach = RosterReportParser.Parse(report).Single();

        dach.IsInjuredReserve.Should().BeTrue();
        dach.BirthDate.Should().Be(new DateTime(2003, 1, 4));
        dach.InjuredReserveDate.Should().Be(new DateTime(2026, 10, 7));
    }

    [TestMethod]
    public void MatchEntries_MatchesByBirthDateAndName()
    {
        var entries = RosterReportParser.Parse(REPORT);
        var candidates = new List<RosterReportCandidate>
        {
            new(1, "One", "Player", new DateTime(2000, 1, 1), 8),
            new(2, "Two", "Player", new DateTime(2000, 1, 2), 8),
            new(18, "A.J.", "Greer", new DateTime(1996, 12, 14), 8),
            new(19, "José", "Åberg", new DateTime(2000, 1, 3), 8),
            new(5, "Five", "Player", new DateTime(2000, 1, 5), 8),
            new(3, "Ian", "Moore", new DateTime(2002, 1, 4), 8),
            // Same birthdate as Ian Moore, different name
            new(30, "Other", "Skater", new DateTime(2002, 1, 4), 6),
        };

        var (statuses, unmatched) = NhlRosterStatusManager.MatchEntries(entries, candidates, new DateTime(2026, 10, 8, 8, 0, 0));

        unmatched.Should().Be(1, "Ryan Ellis has no stats");
        statuses.Select(s => s.PlayerId).Should().BeEquivalentTo([1, 2, 18, 19, 5, 3]);
        statuses.Should().OnlyContain(s => s.TeamId == 8);
        statuses.Single(s => s.PlayerId == 3).IsInjuredReserve.Should().BeTrue();
    }

    [TestMethod]
    public void MatchEntries_SkipsTeamWithTooFewMatches()
    {
        var entries = RosterReportParser.Parse(REPORT);
        var candidates = new List<RosterReportCandidate> { new(1, "One", "Player", new DateTime(2000, 1, 1), 8) };

        var (statuses, unmatched) = NhlRosterStatusManager.MatchEntries(entries, candidates, DateTime.UtcNow);

        statuses.Should().BeEmpty();
        unmatched.Should().Be(7);
    }

    [TestMethod]
    public void MatchEntries_SameBirthDateAndLastName_UsesFirstName()
    {
        var entries = Enumerable.Range(1, 4)
            .Select(i => new ServiceRosterReportEntry("TEAM", $"PLAYER {(char)('A' + i)}", 'C', new DateTime(2000, 1, i), false, null, null))
            .Append(new ServiceRosterReportEntry("TEAM", "ELIAS PETTERSSON", 'C', new DateTime(1998, 11, 12), false, null, null))
            .ToList();
        var candidates = Enumerable.Range(1, 4)
            .Select(i => new RosterReportCandidate(100 + i, "Player", ((char)('A' + i)).ToString(), new DateTime(2000, 1, i), 23))
            .Append(new RosterReportCandidate(1, "Elias", "Pettersson", new DateTime(1998, 11, 12), 23))
            .Append(new RosterReportCandidate(2, "Marcus", "Pettersson", new DateTime(1998, 11, 12), 23))
            .ToList();

        var (statuses, unmatched) = NhlRosterStatusManager.MatchEntries(entries, candidates, DateTime.UtcNow);

        unmatched.Should().Be(0);
        statuses.Select(s => s.PlayerId).Should().Contain(1).And.NotContain(2);
    }
}