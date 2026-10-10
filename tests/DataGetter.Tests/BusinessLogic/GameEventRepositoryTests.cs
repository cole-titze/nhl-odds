using DatabaseAccess.GameEventRepository;
using Entities.DbModels;
using Entities.DbModels.GamePlayEvents;
using FluentAssertions;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class GameEventRepositoryTests
{
    private static List<IDbGameEvent> Shots(int count) =>
        Enumerable.Range(1, count).Select(i => (IDbGameEvent)new DbShot { GameId = 1, Id = i }).ToList();

    private static HashSet<int> Ids(IEnumerable<IDbGameEvent> events) => events.Select(e => e.Id).ToHashSet();

    [TestMethod]
    public void GetStaleEvents_ReturnsRemovedAndRetypedEvents()
    {
        var existing = Shots(10);
        existing.Add(new DbPenalty { GameId = 1, Id = 11 });
        // Shot 3 was re-scored as a miss (same event id) and the penalty was rescinded
        var latest = Shots(10).Where(e => e.Id != 3).ToList();
        latest.Add(new DbMissedShot { GameId = 1, Id = 3 });

        var stale = GameEventRepository.GetStaleEvents(existing, latest, Ids(latest)).ToList();

        stale.Should().HaveCount(2);
        stale.Should().ContainSingle(e => e is DbShot && e.Id == 3);
        stale.Should().ContainSingle(e => e is DbPenalty && e.Id == 11);
    }

    [TestMethod]
    public void GetStaleEvents_KeepsEventsThatAreStillInThePlayByPlayButFailedToMap()
    {
        // Plays 6-10 are in the response but the mapper skipped them
        var stale = GameEventRepository.GetStaleEvents(Shots(10), Shots(5), Ids(Shots(10)));

        stale.Should().BeEmpty();
    }

    [TestMethod]
    public void GetStaleEvents_KeepsEverythingWhenThePlayByPlayLooksTruncated()
    {
        var stale = GameEventRepository.GetStaleEvents(Shots(10), Shots(4), Ids(Shots(4)));

        stale.Should().BeEmpty();
    }

    [TestMethod]
    public void GetStaleEvents_ReturnsNothingWhenUnchanged()
    {
        GameEventRepository.GetStaleEvents(Shots(10), Shots(10), Ids(Shots(10))).Should().BeEmpty();
    }
}
