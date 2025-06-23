using Entities.DbModels;
using Entities.DbModels.GamePlayEvents;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess
{
    public partial class NhlDbContext : DbContext
    {
        private readonly string _connectionString;
        public NhlDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        public virtual DbSet<DbGameRaw> GameRaw { get; set; } = null!;
        public virtual DbSet<DbTeam> Team { get; set; } = null!;
        public virtual DbSet<DbGameOfficial> GameOfficial { get; set; } = null!;
        public virtual DbSet<DbGameSkaterStats> GameSkaterStats { get; set; } = null!;
        public virtual DbSet<DbGameGoalieStats> GameGoalieStats { get; set; } = null!;
        public virtual DbSet<DbPlayer> Player { get; set; } = null!;
        public virtual DbSet<DbPlayerDraftDetails> PlayerDraftDetails { get; set; } = null!;
        public virtual DbSet<DbSeasonGameCount> SeasonGameCount { get; set; } = null!;
        public virtual DbSet<DbGameCleaned> GameCleaned { get; set; } = null!;
        public virtual DbSet<DbGameOdds> GameOdds { get; set; } = null!;
        public virtual DbSet<DbTvBroadcaster> TvBroadcaster { get; set; } = null!;
        public virtual DbSet<DbGameTvBroadcaster> GameTvBroadcaster { get; set; } = null!;

        // Game Event Tables
        public virtual DbSet<DbBlockedShot> BlockedShotEvent { get; set; } = null!;
        public virtual DbSet<DbDelayedPenalty> DelayedPenaltyEvent { get; set; } = null!;
        public virtual DbSet<DbFaceoff> FaceoffEvent { get; set; } = null!;
        public virtual DbSet<DbGameEnd> GameEndEvent { get; set; } = null!;
        public virtual DbSet<DbGiveaway> GiveawayEvent { get; set; } = null!;
        public virtual DbSet<DbGoal> GoalEvent { get; set; } = null!;
        public virtual DbSet<DbHit> HitEvent { get; set; } = null!;
        public virtual DbSet<DbMissedShot> MissedShotEvent { get; set; } = null!;
        public virtual DbSet<DbPenalty> PenaltyEvent { get; set; } = null!;
        public virtual DbSet<DbPeriodStart> PeriodStartEvent { get; set; } = null!;
        public virtual DbSet<DbShot> ShotEvent { get; set; } = null!;
        public virtual DbSet<DbStoppage> StoppageEvent { get; set; } = null!;
        public virtual DbSet<DbTakeaway> TakeawayEvent { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SetEventIds(modelBuilder);

            modelBuilder.Entity<DbGameSkaterStats>()
                .HasKey(c => new { c.gameId, c.playerId });
            modelBuilder.Entity<DbGameGoalieStats>()
                .HasKey(c => new { c.gameId, c.playerId });
            modelBuilder.Entity<DbGameOdds>()
                .HasKey(c => new { c.gameId, c.modelName, c.runDateUTC });
            modelBuilder.Entity<DbGameOfficial>()
                .HasKey(c => new { c.gameId, c.name });
        }
        private void SetEventIds(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbBlockedShot>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbDelayedPenalty>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbFaceoff>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbGameEnd>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbGiveaway>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbGoal>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbHit>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbMissedShot>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbPenalty>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbPeriodStart>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbShot>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbStoppage>()
                .HasKey(c => new { c.id, c.gameId });
            modelBuilder.Entity<DbTakeaway>()
                .HasKey(c => new { c.id, c.gameId });
        }
    }
}