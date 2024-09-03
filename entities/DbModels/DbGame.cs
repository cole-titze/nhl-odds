﻿using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels
{
    public enum Winner
    {
        HOME,
        AWAY
    }
    public class DbGame
    {
        public int id { get; set; } = -1;
        public int homeTeamId { get; set; }
        public int awayTeamId { get; set; }
        public int seasonStartYear { get; set; }
        public DateTime gameDate { get; set; }
        public int homeGoals { get; set; }
        public int awayGoals { get; set; }
        public Winner winner { get; set; }
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
        public bool hasBeenPlayed { get; set; }
        [ForeignKey("homeTeamId")]
        public DbTeam homeTeam { get; set; } = new DbTeam();
        [ForeignKey("awayTeamId")]
        public DbTeam awayTeam { get; set; } = new DbTeam();

        public void Clone(DbGame game)
        {
            id = game.id;
            homeTeamId = game.homeTeamId;
            awayTeamId = game.awayTeamId;
            seasonStartYear = game.seasonStartYear;
            gameDate = game.gameDate;
            homeGoals = game.homeGoals;
            awayGoals = game.awayGoals;
            winner = game.winner;
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
            hasBeenPlayed = game.hasBeenPlayed;
        }
        /// <summary>
        /// Gets whether a game is valid or not
        /// </summary>
        /// <returns>True if both teams won 0 faceoffs</returns>
        public bool IsValid()
        {
            return id != -1;
        }
        /// <summary>
        /// Gets the abbreviation for the team
        /// </summary>
        /// <returns>Three letter abbreviation</returns>
        public string GetTeamAbbr(int teamId)
        {
            if (teamId == homeTeamId)
                return homeTeam.abbreviation;
            if (teamId == awayTeamId)
                return awayTeam.abbreviation;

            return "";
        }
    }
}