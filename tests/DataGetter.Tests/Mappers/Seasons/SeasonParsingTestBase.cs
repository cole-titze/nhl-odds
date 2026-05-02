using System.Text.Json.Nodes;
using Entities.Models.GamePlayEvents;
using Entities.ServiceModels.Mappers;
using Entities.Types.Enums;
using FluentAssertions;

namespace DataGetter.Tests.Mappers.Seasons;

/// <summary>
/// Base class for per-season JSON parsing tests. Each derived class supplies real API
/// fixture data captured from the first regular-season game of that year, ensuring the
/// NHL API response shapes match what the mappers expect across all supported seasons.
/// </summary>
public abstract class SeasonParsingTestBase
{
    protected abstract string BoxscoreJson { get; }
    protected abstract string RightRailJson { get; }
    protected abstract string PlayByPlayJson { get; }

    protected abstract int ExpectedSeasonStartYear { get; }
    protected abstract int ExpectedHomeTeamId { get; }
    protected abstract int ExpectedAwayTeamId { get; }
    protected abstract string ExpectedHomeTeamAbbr { get; }
    protected abstract string ExpectedAwayTeamAbbr { get; }
    protected abstract int ExpectedHomeGoals { get; }
    protected abstract int ExpectedAwayGoals { get; }

    [TestMethod]
    public void MapGameResponse_ParsesGameSummaryFields()
    {
        var game = MapGameResponseToGame.Map(
            JsonNode.Parse(BoxscoreJson),
            JsonNode.Parse(RightRailJson),
            JsonNode.Parse(PlayByPlayJson));

        game.SeasonStartYear.Should().Be(ExpectedSeasonStartYear);
        game.HomeTeamId.Should().Be(ExpectedHomeTeamId);
        game.AwayTeamId.Should().Be(ExpectedAwayTeamId);
        game.HomeTeamAbbr.Should().Be(ExpectedHomeTeamAbbr);
        game.AwayTeamAbbr.Should().Be(ExpectedAwayTeamAbbr);
        game.HasBeenPlayed.Should().BeTrue();
        game.GameType.Should().Be(GameType.Regular);
    }

    [TestMethod]
    public void MapGameResponse_ParsesGoals()
    {
        var game = MapGameResponseToGame.Map(
            JsonNode.Parse(BoxscoreJson),
            JsonNode.Parse(RightRailJson),
            JsonNode.Parse(PlayByPlayJson));

        game.HomeGoals.Should().Be(ExpectedHomeGoals);
        game.AwayGoals.Should().Be(ExpectedAwayGoals);
        game.Winner.Should().Be(ExpectedHomeGoals > ExpectedAwayGoals
            ? Entities.Types.Winner.HOME
            : Entities.Types.Winner.AWAY);
    }

    [TestMethod]
    public void MapGameResponse_ParsesStats()
    {
        var game = MapGameResponseToGame.Map(
            JsonNode.Parse(BoxscoreJson),
            JsonNode.Parse(RightRailJson),
            JsonNode.Parse(PlayByPlayJson));

        game.HomeSOG.Should().BeGreaterThan(0);
        game.AwaySOG.Should().BeGreaterThan(0);
        game.HomePIM.Should().BeGreaterThanOrEqualTo(0);
        game.AwayPIM.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public void MapPlayByPlay_ParsesEventsWithoutThrowing()
    {
        var events = MapGameEventsResponseToGameEvents.Map(JsonNode.Parse(PlayByPlayJson)).Events.ToList();

        events.Should().NotBeEmpty();
        events.OfType<PeriodStart>().Should().NotBeEmpty();
        events.OfType<Faceoff>().Should().NotBeEmpty();
        events.OfType<Goal>().Should().NotBeEmpty();
    }
}
