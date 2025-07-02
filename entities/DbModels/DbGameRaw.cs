using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.DbModels;

public class DbGameRaw
{
    public int Id { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int SeasonStartYear { get; set; }
    public DateTime GameDateUTC { get; set; }
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
    public Winner Winner { get; set; }
    public PeriodType EndPeriod { get; set; }
    public int HomeSOG { get; set; }
    public int AwaySOG { get; set; }
    public int HomePPG { get; set; }
    public int AwayPPG { get; set; }
    public int HomePIM { get; set; }
    public int AwayPIM { get; set; }
    public double HomeFaceOffWinPercent { get; set; }
    public double AwayFaceOffWinPercent { get; set; }
    public int HomeBlockedShots { get; set; }
    public int AwayBlockedShots { get; set; }
    public int HomeHits { get; set; }
    public int AwayHits { get; set; }
    public int HomeTakeaways { get; set; }
    public int AwayTakeaways { get; set; }
    public int HomeGiveaways { get; set; }
    public int AwayGiveaways { get; set; }
    public bool HasBeenPlayed { get; set; }
    public string GameSummary { get; set; } = string.Empty;
    public string EventSummary { get; set; } = string.Empty;
    public string PlayByPlaySummary { get; set; } = string.Empty;
    public string FaceoffSummary { get; set; } = string.Empty;
    public string FaceoffComparisonSummary { get; set; } = string.Empty;
    public string RosterSummary { get; set; } = string.Empty;
    public string ShotSummary { get; set; } = string.Empty;
    public string ShiftChartSummary { get; set; } = string.Empty;
    public string ToiAwaySummary { get; set; } = string.Empty;
    public string ToiHomeSummary { get; set; } = string.Empty;
    public int ThreeMinuteRecapVideoId { get; set; }
    public int CondensedGameVideoId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string VenueLocation { get; set; } = string.Empty;
    [ForeignKey(nameof(HomeTeamId))]
    public DbTeam? HomeTeam { get; set; }
    [ForeignKey(nameof(AwayTeamId))]
    public DbTeam? AwayTeam { get; set; }

    public void Clone(DbGameRaw game)
    {
        Id = game.Id;
        HomeTeamId = game.HomeTeamId;
        AwayTeamId = game.AwayTeamId;
        SeasonStartYear = game.SeasonStartYear;
        GameDateUTC = game.GameDateUTC;
        HomeGoals = game.HomeGoals;
        AwayGoals = game.AwayGoals;
        Winner = game.Winner;
        EndPeriod = game.EndPeriod;
        HomeSOG = game.HomeSOG;
        AwaySOG = game.AwaySOG;
        HomePPG = game.HomePPG;
        AwayPPG = game.AwayPPG;
        HomePIM = game.HomePIM;
        AwayPIM = game.AwayPIM;
        HomeFaceOffWinPercent = game.HomeFaceOffWinPercent;
        AwayFaceOffWinPercent = game.AwayFaceOffWinPercent;
        HomeBlockedShots = game.HomeBlockedShots;
        AwayBlockedShots = game.AwayBlockedShots;
        HomeHits = game.HomeHits;
        AwayHits = game.AwayHits;
        HomeTakeaways = game.HomeTakeaways;
        AwayTakeaways = game.AwayTakeaways;
        HomeGiveaways = game.HomeGiveaways;
        AwayGiveaways = game.AwayGiveaways;
        HasBeenPlayed = game.HasBeenPlayed;
        HomeTeam = game.HomeTeam;
        AwayTeam = game.AwayTeam;
        GameSummary = game.GameSummary;
        EventSummary = game.EventSummary;
        PlayByPlaySummary = game.PlayByPlaySummary;
        FaceoffSummary = game.FaceoffSummary;
        FaceoffComparisonSummary = game.FaceoffComparisonSummary;
        RosterSummary = game.RosterSummary;
        ShotSummary = game.ShotSummary;
        ShiftChartSummary = game.ShiftChartSummary;
        ToiAwaySummary = game.ToiAwaySummary;
        ToiHomeSummary = game.ToiHomeSummary;
        ThreeMinuteRecapVideoId = game.ThreeMinuteRecapVideoId;
        CondensedGameVideoId = game.CondensedGameVideoId;
        VenueName = game.VenueName;
        VenueLocation = game.VenueLocation;
    }
    public bool IsEquivalentTo(DbGameRaw? other)
    {
        if (other == null)
            return false;

        return Id == other.Id
            && HomeTeamId == other.HomeTeamId
            && AwayTeamId == other.AwayTeamId
            && SeasonStartYear == other.SeasonStartYear
            && GameDateUTC == other.GameDateUTC
            && HomeGoals == other.HomeGoals
            && AwayGoals == other.AwayGoals
            && Winner == other.Winner
            && EndPeriod == other.EndPeriod
            && HomeSOG == other.HomeSOG
            && AwaySOG == other.AwaySOG
            && HomePPG == other.HomePPG
            && AwayPPG == other.AwayPPG
            && HomePIM == other.HomePIM
            && AwayPIM == other.AwayPIM
            && HomeFaceOffWinPercent.Equals(other.HomeFaceOffWinPercent)
            && AwayFaceOffWinPercent.Equals(other.AwayFaceOffWinPercent)
            && HomeBlockedShots == other.HomeBlockedShots
            && AwayBlockedShots == other.AwayBlockedShots
            && HomeHits == other.HomeHits
            && AwayHits == other.AwayHits
            && HomeTakeaways == other.HomeTakeaways
            && AwayTakeaways == other.AwayTakeaways
            && HomeGiveaways == other.HomeGiveaways
            && AwayGiveaways == other.AwayGiveaways
            && HasBeenPlayed == other.HasBeenPlayed
            && GameSummary == other.GameSummary
            && EventSummary == other.EventSummary
            && PlayByPlaySummary == other.PlayByPlaySummary
            && FaceoffSummary == other.FaceoffSummary
            && FaceoffComparisonSummary == other.FaceoffComparisonSummary
            && RosterSummary == other.RosterSummary
            && ShotSummary == other.ShotSummary
            && ShiftChartSummary == other.ShiftChartSummary
            && ToiAwaySummary == other.ToiAwaySummary
            && ToiHomeSummary == other.ToiHomeSummary
            && ThreeMinuteRecapVideoId == other.ThreeMinuteRecapVideoId
            && CondensedGameVideoId == other.CondensedGameVideoId
            && VenueName == other.VenueName
            && VenueLocation == other.VenueLocation;
    }
}