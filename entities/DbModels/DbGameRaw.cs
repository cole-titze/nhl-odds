using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.DbModels
{
    public class DbGameRaw
    {
        public int id { get; set; }
        public int homeTeamId { get; set; }
        public int awayTeamId { get; set; }
        public int seasonStartYear { get; set; }
        public DateTime gameDateUTC { get; set; }
        public int homeGoals { get; set; }
        public int awayGoals { get; set; }
        public Winner winner { get; set; }
        public PeriodType endPeriod { get; set; }
        public int homeSOG { get; set; }
        public int awaySOG { get; set; }
        public int homePPG { get; set; }
        public int awayPPG { get; set; }
        public int homePIM { get; set; }
        public int awayPIM { get; set; }
        public double homeFaceOffWinPercent { get; set; }
        public double awayFaceOffWinPercent { get; set; }
        public int homeBlockedShots { get; set; }
        public int awayBlockedShots { get; set; }
        public int homeHits { get; set; }
        public int awayHits { get; set; }
        public int homeTakeaways { get; set; }
        public int awayTakeaways { get; set; }
        public int homeGiveaways { get; set; }
        public int awayGiveaways { get; set; }
        public bool hasBeenPlayed { get; set; }
        public string gameSummary { get; set; } = string.Empty;
        public string eventSummary { get; set; } = string.Empty;
        public string playByPlaySummary { get; set; } = string.Empty;
        public string faceoffSummary { get; set; } = string.Empty;
        public string faceoffComparisonSummary { get; set; } = string.Empty;
        public string rosterSummary { get; set; } = string.Empty;
        public string shotSummary { get; set; } = string.Empty;
        public string shiftChartSummary { get; set; } = string.Empty;
        public string toiAwaySummary { get; set; } = string.Empty;
        public string toiHomeSummary { get; set; } = string.Empty;
        public int threeMinuteRecapVideoId { get; set; }
        public int condensedGameVideoId { get; set; }
        public string venueName { get; set; } = string.Empty;
        public string venueLocation { get; set; } = string.Empty;

        [ForeignKey(nameof(homeTeamId))]
        public DbTeam? homeTeam { get; set; }
        [ForeignKey(nameof(awayTeamId))]
        public DbTeam? awayTeam { get; set; }

        public void Clone(DbGameRaw game)
        {
            id = game.id;
            homeTeamId = game.homeTeamId;
            awayTeamId = game.awayTeamId;
            seasonStartYear = game.seasonStartYear;
            gameDateUTC = game.gameDateUTC;
            homeGoals = game.homeGoals;
            awayGoals = game.awayGoals;
            winner = game.winner;
            endPeriod = game.endPeriod;
            homeSOG = game.homeSOG;
            awaySOG = game.awaySOG;
            homePPG = game.homePPG;
            awayPPG = game.awayPPG;
            homePIM = game.homePIM;
            awayPIM = game.awayPIM;
            homeFaceOffWinPercent = game.homeFaceOffWinPercent;
            awayFaceOffWinPercent = game.awayFaceOffWinPercent;
            homeBlockedShots = game.homeBlockedShots;
            awayBlockedShots = game.awayBlockedShots;
            homeHits = game.homeHits;
            awayHits = game.awayHits;
            homeTakeaways = game.homeTakeaways;
            awayTakeaways = game.awayTakeaways;
            homeGiveaways = game.homeGiveaways;
            awayGiveaways = game.awayGiveaways;
            hasBeenPlayed = game.hasBeenPlayed;
            homeTeam = game.homeTeam;
            awayTeam = game.awayTeam;
            gameSummary = game.gameSummary;
            eventSummary = game.eventSummary;
            playByPlaySummary = game.playByPlaySummary;
            faceoffSummary = game.faceoffSummary;
            faceoffComparisonSummary = game.faceoffComparisonSummary;
            rosterSummary = game.rosterSummary;
            shotSummary = game.shotSummary;
            shiftChartSummary = game.shiftChartSummary;
            toiAwaySummary = game.toiAwaySummary;
            toiHomeSummary = game.toiHomeSummary;
            threeMinuteRecapVideoId = game.threeMinuteRecapVideoId;
            condensedGameVideoId = game.condensedGameVideoId;
            venueName = game.venueName;
            venueLocation = game.venueLocation;
        }
        /// <summary>
        /// Gets the abbreviation for the team
        /// </summary>
        /// <returns>Three letter abbreviation</returns>
        public string GetTeamAbbr(int teamId)
        {
            if (teamId == homeTeamId)
                return homeTeam?.abbreviation ?? "";
            if (teamId == awayTeamId)
                return awayTeam?.abbreviation ?? "";

            return "";
        }
    }
}