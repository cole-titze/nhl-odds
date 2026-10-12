using DataCleaner.Mappers;
using DataCleaner.Tests.Helpers;
using Entities.DbModels;
using Entities.Models;
using Entities.Types.Enums;

namespace DataCleaner.Tests.Mappers;

public class MapScheduleToDbGameCleanedTests
{
    private static readonly DateTime BaseDate = new(2024, 10, 10, 23, 0, 0, DateTimeKind.Utc);

    private static DbGameCleaned Apply(Game game, params Game[] games)
    {
        var cleaned = new DbGameCleaned { GameId = game.Id };
        MapScheduleToDbGameCleaned.Apply(cleaned, game, new SeasonGames(games.Append(game)));
        return cleaned;
    }

    [Fact]
    public void DistanceKm_SameCity_IsZero()
    {
        Assert.Equal(0, MapScheduleToDbGameCleaned.DistanceKm("Boston", "Boston"));
    }

    [Fact]
    public void DistanceKm_BostonToVancouver_IsAbout4000Km()
    {
        Assert.InRange(MapScheduleToDbGameCleaned.DistanceKm("Boston", "Vancouver"), 4000, 4100);
    }

    [Fact]
    public void Apply_Travel_CountsTripsDistanceAndTimeZones()
    {
        // Team 1 plays out of Vancouver, team 2 out of Boston. Team 1 opens at home, then both games are in Boston.
        var first = new GameBuilder().WithId(1).WithTeams(1, 2).WithDate(BaseDate).WithVenue("Vancouver").Build();
        var second = new GameBuilder().WithId(2).WithTeams(2, 1).WithDate(BaseDate.AddDays(2)).WithVenue("Boston").Build();
        // Upcoming game: no venue yet, so it's the home team's city
        var target = new GameBuilder().WithId(3).WithTeams(2, 1).WithDate(BaseDate.AddDays(3)).Unplayed().Build();
        var km = MapScheduleToDbGameCleaned.DistanceKm("Vancouver", "Boston");

        var result = Apply(target, first, second);

        Assert.Equal(0, result.TravelKmAdvantage); // both stayed in Boston
        // Team 2 flew Boston -> Vancouver -> Boston; team 1 only Vancouver -> Boston
        Assert.Equal(km - 2 * km, result.Km7dAdvantage, 6);
        Assert.Equal(0, result.Games7dAdvantage);
        Assert.Equal(2, result.HomeTripLength);
        Assert.Equal(2, result.AwayTripLength);
        Assert.Equal(3, result.AwayTimeZoneShift); // Boston is 3 hours ahead of Vancouver
        Assert.Equal(15, result.AwayBodyClockStartHour); // 23:00 UTC is 15:00 in Vancouver
    }

    [Fact]
    public void Apply_Travel_OldGamesDropOutOfSevenDays()
    {
        var first = new GameBuilder().WithId(1).WithTeams(1, 2).WithDate(BaseDate).WithVenue("Vancouver").Build();
        var second = new GameBuilder().WithId(2).WithTeams(2, 1).WithDate(BaseDate.AddDays(2)).WithVenue("Boston").Build();
        var target = new GameBuilder().WithId(3).WithTeams(2, 1).WithDate(BaseDate.AddDays(10)).WithVenue("Boston").Build();

        var result = Apply(target, first, second);

        Assert.Equal(0, result.Games7dAdvantage);
        Assert.Equal(0, result.Km7dAdvantage);
    }

    [Fact]
    public void Apply_LastSeason_UsesGoalDiffAndPointsWithOvertimeLossPoint()
    {
        var lastSeason = new DateTime(2023, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var win = new GameBuilder().WithId(1).WithSeasonStartYear(2023).WithTeams(1, 2).WithDate(lastSeason)
            .WithScore(4, 1).Build();
        var overtimeLoss = new GameBuilder().WithId(2).WithSeasonStartYear(2023).WithTeams(1, 2)
            .WithDate(lastSeason.AddDays(1)).WithScore(2, 3).WithEndPeriod(PeriodType.Overtime).Build();
        var target = new GameBuilder().WithId(3).WithTeams(1, 2).WithDate(BaseDate).Unplayed().Build();

        var result = Apply(target, win, overtimeLoss);

        // Team 1: goal diff (3 - 1) / 2 = 1, points (2 + 1) / 4 = 0.75. Team 2: -1 and (0 + 2) / 4 = 0.5.
        Assert.Equal(2, result.PrevGoalDiffAdvantage, 6);
        Assert.Equal(0.25, result.PrevPointsPctAdvantage, 6);
        // No games this season: last season's goal diff at two thirds strength
        Assert.Equal(4.0 / 3, result.BlendGoalDiffAdvantage, 6);
        Assert.Equal(0, result.MinGamesPlayed);
    }

    [Fact]
    public void Apply_BlendGoalDiff_MovesTowardThisSeason()
    {
        var lastSeason = new GameBuilder().WithId(1).WithSeasonStartYear(2023).WithTeams(1, 2)
            .WithDate(new DateTime(2023, 11, 1, 0, 0, 0, DateTimeKind.Utc)).WithScore(4, 1).Build();
        var thisSeason = new GameBuilder().WithId(2).WithTeams(2, 1).WithDate(BaseDate).WithScore(5, 0).Build();
        var target = new GameBuilder().WithId(3).WithTeams(1, 2).WithDate(BaseDate.AddDays(2)).Unplayed().Build();

        var result = Apply(target, lastSeason, thisSeason);

        // Team 1: (1 * -5 + 20 * 3 * 2/3) / 21; team 2 mirrors it
        var team1 = (-5 + 20 * 2.0) / 21;
        Assert.Equal(2 * team1, result.BlendGoalDiffAdvantage, 6);
        Assert.Equal(1, result.MinGamesPlayed);
    }

    [Fact]
    public void Apply_RelocatedTeam_UsesPredecessorsLastSeason()
    {
        // Winnipeg (52) took over Atlanta (11) in 2011
        var lastSeason = new GameBuilder().WithId(1).WithSeasonStartYear(2010).WithTeams(11, 2)
            .WithDate(new DateTime(2010, 11, 1, 0, 0, 0, DateTimeKind.Utc)).WithScore(3, 1).Build();
        var target = new GameBuilder().WithId(2).WithSeasonStartYear(2011).WithTeams(52, 2)
            .WithDate(new DateTime(2011, 10, 10, 0, 0, 0, DateTimeKind.Utc)).Unplayed().Build();

        var result = Apply(target, lastSeason);

        Assert.Equal(4, result.PrevGoalDiffAdvantage, 6);
    }

    [Fact]
    public void Apply_NoLastSeason_LeavesPrevFeaturesAtZero()
    {
        var target = new GameBuilder().WithId(1).WithTeams(1, 2).WithDate(BaseDate).WithVenue("Boston").Build();

        var result = Apply(target);

        Assert.Equal(0, result.PrevGoalDiffAdvantage);
        Assert.Equal(0, result.PrevPointsPctAdvantage);
        Assert.Equal(0, result.BlendGoalDiffAdvantage);
    }
}
