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
    public virtual DbSet<DbErrorLog> ErrorLog { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbBookmakerOdds>()
            .HasKey(c => new { c.GameId, c.BookmakerName });
        modelBuilder.Entity<DbGameOdds>()
            .HasKey(c => new { c.GameId, c.ModelId, c.RunDateUTC });
        modelBuilder.Entity<DbSeasonTeam>()
            .HasKey(c => new { c.TeamId, c.SeasonStartYear });
    }
}
