using Entities.DbModels;
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbGameSkaterStats>()
                .HasKey(c => new { c.gameId, c.playerId });
            modelBuilder.Entity<DbGameGoalieStats>()
                .HasKey(c => new { c.gameId, c.playerId });
            modelBuilder.Entity<DbGameOdds>()
                .HasKey(c => new { c.gameId, c.modelName, c.runDateUTC });
        }
    }
}