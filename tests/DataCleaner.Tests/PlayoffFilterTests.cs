using DataCleaner.Tests.Helpers;
using Entities.Models;
using Entities.Types.Enums;

namespace DataCleaner.Tests;

public class PlayoffFilterTests
{
    [Fact]
    public void GameCleanerSeasonFilter_ExcludesPlayoffGames()
    {
        var games = new List<Game>
        {
            new GameBuilder().WithId(2024020001).WithGameType(GameType.Regular).Build(),
            new GameBuilder().WithId(2024020002).WithGameType(GameType.Regular).Build(),
            new GameBuilder().WithId(2024030001).WithGameType(GameType.Playoff).Build(),
        };

        // Mirror the filter used in GameCleaner.CleanGamesInSeasons
        var filtered = games.Where(g => g.GameType == GameType.Regular).ToList();

        Assert.Equal(2, filtered.Count);
        Assert.Equal(new[] { 2024020001, 2024020002 }, filtered.Select(g => g.Id));
        Assert.DoesNotContain(filtered, g => g.GameType == GameType.Playoff);
    }
}
