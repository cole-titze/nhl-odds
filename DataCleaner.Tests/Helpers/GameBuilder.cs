using Entities.Models;
using Entities.Types;
using Entities.Types.Enums;

namespace DataCleaner.Tests.Helpers;

public class GameBuilder
{
    private int _id = 1;
    private int _homeTeamId = 1;
    private int _awayTeamId = 2;
    private int _seasonStartYear = 2024;
    private DateTime _gameDate = new(2024, 10, 10, 19, 0, 0);
    private int _homeGoals = 3;
    private int _awayGoals = 2;
    private Winner _winner = Winner.HOME;
    private PeriodType _endPeriod = PeriodType.Regulation;
    private int _homeSOG = 30;
    private int _awaySOG = 28;
    private int _homePPG = 1;
    private int _awayPPG = 0;
    private int _homePIM = 8;
    private int _awayPIM = 10;
    private double _homeFaceOffWinPct = 52.0;
    private double _awayFaceOffWinPct = 48.0;
    private int _homeBlockedShots = 15;
    private int _awayBlockedShots = 12;
    private int _homeHits = 20;
    private int _awayHits = 22;
    private int _homeTakeaways = 5;
    private int _awayTakeaways = 4;
    private int _homeGiveaways = 7;
    private int _awayGiveaways = 6;
    private bool _hasBeenPlayed = true;

    public GameBuilder WithId(int id) { _id = id; return this; }
    public GameBuilder WithTeams(int home, int away) { _homeTeamId = home; _awayTeamId = away; return this; }
    public GameBuilder WithDate(DateTime date) { _gameDate = date; return this; }
    public GameBuilder WithScore(int homeGoals, int awayGoals)
    {
        _homeGoals = homeGoals;
        _awayGoals = awayGoals;
        _winner = homeGoals > awayGoals ? Winner.HOME : Winner.AWAY;
        return this;
    }
    public GameBuilder WithWinner(Winner winner) { _winner = winner; return this; }
    public GameBuilder WithEndPeriod(PeriodType endPeriod) { _endPeriod = endPeriod; return this; }
    public GameBuilder WithSOG(int home, int away) { _homeSOG = home; _awaySOG = away; return this; }
    public GameBuilder WithHits(int home, int away) { _homeHits = home; _awayHits = away; return this; }
    public GameBuilder WithPPG(int home, int away) { _homePPG = home; _awayPPG = away; return this; }
    public GameBuilder WithPIM(int home, int away) { _homePIM = home; _awayPIM = away; return this; }
    public GameBuilder WithFaceOffPct(double home, double away) { _homeFaceOffWinPct = home; _awayFaceOffWinPct = away; return this; }
    public GameBuilder WithBlockedShots(int home, int away) { _homeBlockedShots = home; _awayBlockedShots = away; return this; }
    public GameBuilder WithTakeaways(int home, int away) { _homeTakeaways = home; _awayTakeaways = away; return this; }
    public GameBuilder WithGiveaways(int home, int away) { _homeGiveaways = home; _awayGiveaways = away; return this; }
    public GameBuilder WithSeasonStartYear(int year) { _seasonStartYear = year; return this; }

    public Game Build() => new()
    {
        Id = _id,
        HomeTeamId = _homeTeamId,
        AwayTeamId = _awayTeamId,
        SeasonStartYear = _seasonStartYear,
        GameDateUTC = _gameDate,
        HomeGoals = _homeGoals,
        AwayGoals = _awayGoals,
        Winner = _winner,
        EndPeriod = _endPeriod,
        HomeSOG = _homeSOG,
        AwaySOG = _awaySOG,
        HomePPG = _homePPG,
        AwayPPG = _awayPPG,
        HomePIM = _homePIM,
        AwayPIM = _awayPIM,
        HomeFaceOffWinPercent = _homeFaceOffWinPct,
        AwayFaceOffWinPercent = _awayFaceOffWinPct,
        HomeBlockedShots = _homeBlockedShots,
        AwayBlockedShots = _awayBlockedShots,
        HomeHits = _homeHits,
        AwayHits = _awayHits,
        HomeTakeaways = _homeTakeaways,
        AwayTakeaways = _awayTakeaways,
        HomeGiveaways = _homeGiveaways,
        AwayGiveaways = _awayGiveaways,
        HasBeenPlayed = _hasBeenPlayed,
    };
}
