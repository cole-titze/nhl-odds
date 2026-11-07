using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess;

public partial class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }
    public virtual DbSet<DbGameRaw> GameRaw { get; set; } = null!;
    public virtual DbSet<DbSeasonTeam> SeasonTeam { get; set; } = null!;
    public virtual DbSet<DbGameOdds> GameOdds { get; set; } = null!;
    public virtual DbSet<DbBookmakerOdds> BookmakerOdds { get; set; } = null!;
    public virtual DbSet<DbBookmakerClosingOdds> BookmakerClosingOdds { get; set; } = null!;
    public virtual DbSet<DbPartnerOdds> PartnerOdds { get; set; } = null!;
    public virtual DbSet<DbBookmakerSpreads> BookmakerSpreads { get; set; } = null!;
    public virtual DbSet<DbBookmakerTotals> BookmakerTotals { get; set; } = null!;
    public virtual DbSet<DbErrorLog> ErrorLog { get; set; } = null!;
    public virtual DbSet<DbGameCleaned> GameCleaned { get; set; } = null!;
    public virtual DbSet<DbBookmakerOddsResponse> BookmakerOddsResponse { get; set; } = null!;
    public virtual DbSet<DbGameSpreadTotalOdds> GameSpreadTotalOdds { get; set; } = null!;
    public virtual DbSet<DbGamePredictionFactor> GamePredictionFactor { get; set; } = null!;
    public virtual DbSet<DbJobStatus> JobStatus { get; set; } = null!;
    public virtual DbSet<DbLoadTestRun> LoadTestRun { get; set; } = null!;
    public virtual DbSet<DbLoadTestRunPage> LoadTestRunPage { get; set; } = null!;
    public virtual DbSet<DbUptimeDay> UptimeDay { get; set; } = null!;
    public virtual DbSet<DbLineupGame> LineupGame { get; set; } = null!;
    public virtual DbSet<DbLineupPlayer> LineupPlayer { get; set; } = null!;
    public virtual DbSet<DbPlayer> Player { get; set; } = null!;
    public virtual DbSet<DbGameSkaterStats> GameSkaterStats { get; set; } = null!;
    public virtual DbSet<DbGameGoalieStats> GameGoalieStats { get; set; } = null!;
    public virtual DbSet<DbGameRosterSpot> GameRosterSpot { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbBookmakerOdds>()
            .HasKey(c => new { c.GameId, c.BookmakerName });
        modelBuilder.Entity<DbBookmakerClosingOdds>()
            .HasKey(c => new { c.GameId, c.BookmakerName });
        modelBuilder.Entity<DbPartnerOdds>()
            .HasKey(c => new { c.Country, c.PartnerUpdatedUTC, c.GameId, c.TeamId, c.Market, c.Qualifier });
        modelBuilder.Entity<DbBookmakerSpreads>()
            .HasKey(c => new { c.GameId, c.BookmakerName });
        modelBuilder.Entity<DbBookmakerTotals>()
            .HasKey(c => new { c.GameId, c.BookmakerName });
        modelBuilder.Entity<DbGameOdds>()
            .HasKey(c => new { c.GameId, c.ModelId, c.RunDateUTC });
        modelBuilder.Entity<DbGameSpreadTotalOdds>()
            .HasKey(c => new { c.GameId, c.ModelId, c.RunDateUTC });
        modelBuilder.Entity<DbGamePredictionFactor>()
            .HasKey(c => new { c.GameId, c.ModelId, c.RunDateUTC, c.Rank });
        modelBuilder.Entity<DbLoadTestRunPage>()
            .HasKey(c => new { c.LoadTestRunId, c.Page });
        modelBuilder.Entity<DbLineupGame>()
            .HasKey(c => c.SectionHash);
        modelBuilder.Entity<DbLineupPlayer>()
            .HasKey(c => new { c.SectionHash, c.Side, c.Group, c.LineNumber, c.Slot });
        modelBuilder.Entity<DbSeasonTeam>()
            .HasKey(c => new { c.TeamId, c.SeasonStartYear });
        modelBuilder.Entity<DbGameSkaterStats>()
            .HasKey(c => new { c.GameId, c.PlayerId });
        modelBuilder.Entity<DbGameGoalieStats>()
            .HasKey(c => new { c.GameId, c.PlayerId });
        modelBuilder.Entity<DbGameRosterSpot>()
            .HasKey(c => new { c.GameId, c.PlayerId });
    }
}