using Entities.DbModels;
using Entities.DbModels.GamePlayEvents;

using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess;

public partial class NhlDbContext : DbContext
{
    private readonly string _connectionString;
    public NhlDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }
    public virtual DbSet<DbGameRaw> GameRaw { get; set; } = null!;
    public virtual DbSet<DbTeam> Team { get; set; } = null!;
    public virtual DbSet<DbSeasonTeam> SeasonTeam { get; set; } = null!;
    public virtual DbSet<DbGameOfficial> GameOfficial { get; set; } = null!;
    public virtual DbSet<DbGameCoach> GameCoach { get; set; } = null!;
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
    public virtual DbSet<DbBlockedShot> GameBlockedShotEvent { get; set; } = null!;
    public virtual DbSet<DbDelayedPenalty> GameDelayedPenaltyEvent { get; set; } = null!;
    public virtual DbSet<DbFaceoff> GameFaceoffEvent { get; set; } = null!;
    public virtual DbSet<DbGameEnd> GameGameEndEvent { get; set; } = null!;
    public virtual DbSet<DbGiveaway> GameGiveawayEvent { get; set; } = null!;
    public virtual DbSet<DbGoal> GameGoalEvent { get; set; } = null!;
    public virtual DbSet<DbHit> GameHitEvent { get; set; } = null!;
    public virtual DbSet<DbMissedShot> GameMissedShotEvent { get; set; } = null!;
    public virtual DbSet<DbPenalty> GamePenaltyEvent { get; set; } = null!;
    public virtual DbSet<DbPeriodStart> GamePeriodStartEvent { get; set; } = null!;
    public virtual DbSet<DbPeriodEnd> GamePeriodEndEvent { get; set; } = null!;
    public virtual DbSet<DbShot> GameShotEvent { get; set; } = null!;
    public virtual DbSet<DbStoppage> GameStoppageEvent { get; set; } = null!;
    public virtual DbSet<DbTakeaway> GameTakeawayEvent { get; set; } = null!;
    public virtual DbSet<DbShootoutComplete> GameShootoutCompleteEvent { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_connectionString);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        SetEventIds(modelBuilder);

        modelBuilder.Entity<DbGameSkaterStats>()
            .HasKey(c => new { c.GameId, c.PlayerId });
        modelBuilder.Entity<DbGameGoalieStats>()
            .HasKey(c => new { c.GameId, c.PlayerId });
        modelBuilder.Entity<DbGameOdds>()
            .HasKey(c => new { c.GameId, c.ModelName, c.RunDateUTC });
        modelBuilder.Entity<DbGameOfficial>()
            .HasKey(c => new { c.GameId, c.Name });
        modelBuilder.Entity<DbGameCoach>()
            .HasKey(c => new { c.GameId, c.Name });
        modelBuilder.Entity<DbSeasonTeam>()
            .HasKey(c => new { c.TeamId, c.SeasonStartYear });
        modelBuilder.Entity<DbGameTvBroadcaster>()
            .HasKey(c => new { c.GameId, c.BroadcasterId });
    }
    private void SetEventIds(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbBlockedShot>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbDelayedPenalty>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbFaceoff>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbGameEnd>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbGiveaway>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbGoal>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbHit>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbMissedShot>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbPenalty>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbPeriodStart>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbPeriodEnd>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbShot>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbStoppage>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbTakeaway>()
            .HasKey(c => new { c.Id, c.GameId });
        modelBuilder.Entity<DbShootoutComplete>()
            .HasKey(c => new { c.Id, c.GameId });
    }
}
