using DataGetter.BusinessLogic;
using FluentAssertions;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlSeasonArticlesTests
{
    [TestMethod]
    public void Season_StartsJulyFirst()
    {
        NhlSeasonArticles.Season(new DateTime(2027, 6, 30)).Should().Be("2026-27");
        NhlSeasonArticles.Season(new DateTime(2027, 7, 1)).Should().Be("2027-28");
        NhlSeasonArticles.Season(new DateTime(2099, 10, 1)).Should().Be("2099-00");
        NhlSeasonArticles.PreviousSeason("2027-28").Should().Be("2026-27");
    }

    [TestMethod]
    public void TrackerSlugs_CoverThisSeasonAndLast()
    {
        NhlSeasonArticles.TrackerSlugs(new DateTime(2027, 7, 1)).Should().Equal(
            "2026-27-nhl-trades", "free-agency-signings-nhl-2026-27", "2027-28-nhl-trades", "free-agency-signings-nhl-2027-28");
    }

    [TestMethod]
    public void IsPropsSlug_MatchesBothSeasonsNames()
    {
        NhlSeasonArticles.IsPropsSlug("nhl-picks-props-daily-fantasy-hockey-projections-for-2026-27-season").Should().BeTrue();
        NhlSeasonArticles.IsPropsSlug("fantasy-hockey-picks-props-projections-for-2025-26-season").Should().BeTrue();
        NhlSeasonArticles.IsPropsSlug("nhl-stanley-cup-playoffs-fantasy-hockey-sleeper-picks-2026").Should().BeFalse();
    }
}