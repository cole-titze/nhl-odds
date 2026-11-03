using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.SearchSourceRepository;

public class SearchSourceRepository(NhlDbContext db) : ISearchSourceRepository
{
    public Task<List<DbSeasonTeam>> GetSeasonTeams(CancellationToken ct = default) =>
        db.SeasonTeam.AsNoTracking().ToListAsync(ct);

    public async Task<List<(DbPlayer Player, string? Position)>> GetPlayers(CancellationToken ct = default)
    {
        // Only the columns the index uses (several others are nullable in the table)
        var players = await db.Player.AsNoTracking()
            .Select(p => new DbPlayer
            {
                Id = p.Id, FirstName = p.FirstName, LastName = p.LastName, IsActive = p.IsActive,
                CurrentTeamId = p.CurrentTeamId, HeadShot = p.HeadShot,
            })
            .ToListAsync(ct);
        var positions = await db.GameRosterSpot.AsNoTracking()
            .GroupBy(r => r.PlayerId)
            .Select(g => new { PlayerId = g.Key, Position = g.OrderByDescending(r => r.GameId).Select(r => r.PositionCode).First() })
            .ToDictionaryAsync(p => p.PlayerId, p => p.Position, ct);
        return players.Select(p => (p, positions.GetValueOrDefault(p.Id))).ToList();
    }

    public Task<List<DbGameRaw>> GetGames(CancellationToken ct = default) =>
        // Only the columns the index uses: the summaries are large
        db.GameRaw.AsNoTracking()
            .Select(g => new DbGameRaw
            {
                Id = g.Id, HomeTeamId = g.HomeTeamId, AwayTeamId = g.AwayTeamId, SeasonStartYear = g.SeasonStartYear,
                GameDateUTC = g.GameDateUTC, HomeGoals = g.HomeGoals, AwayGoals = g.AwayGoals,
                HasBeenPlayed = g.HasBeenPlayed, GameType = g.GameType,
            })
            .ToListAsync(ct);

    public Task<List<DbNhlArticle>> GetArticles(CancellationToken ct = default) =>
        // Newest version only (a ROW_NUMBER() query): rewritten-in-place articles like the trade tracker have many
        db.NhlArticle.AsNoTracking()
            .GroupBy(a => a.EntityId)
            .Select(g => g.OrderByDescending(a => a.FirstSeenUTC).First())
            .ToListAsync(ct);
}
