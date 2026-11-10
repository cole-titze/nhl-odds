using Entities.DbModels;
using Entities.Types;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.SearchSourceRepository;

public class SearchSourceRepository(NhlDbContext db) : ISearchSourceRepository
{
    public Task<List<DbSeasonTeam>> GetSeasonTeams(CancellationToken ct = default) =>
        db.SeasonTeam.AsNoTracking().ToListAsync(ct);

    // NhlAdd saves a roster report snapshot daily; an older one means the job or the report is down. The Player
    // page's rule (#170), shared so a player's team in search and on their page can't drift apart
    public static readonly TimeSpan RosterReportMaxAge = WebPlayerRepository.WebPlayerRepository.RosterReportMaxAge;

    public async Task<List<(DbPlayer Player, string? Position, int? CurrentTeamId)>> GetPlayers(DateTime nowUtc, CancellationToken ct = default)
    {
        // Only the columns the index uses (several others are nullable in the table)
        var players = await db.Player.AsNoTracking()
            .Select(p => new DbPlayer
            {
                Id = p.Id, FirstName = p.FirstName, LastName = p.LastName, IsActive = p.IsActive, HeadShot = p.HeadShot,
            })
            .ToListAsync(ct);
        var positions = await db.GameRosterSpot.AsNoTracking()
            .GroupBy(r => r.PlayerId)
            .Select(g => new { PlayerId = g.Key, Position = g.OrderByDescending(r => r.GameId).Select(r => r.PositionCode).First() })
            .ToDictionaryAsync(p => p.PlayerId, p => p.Position, ct);
        var currentTeams = await GetCurrentTeams(nowUtc, ct);
        return players
            .Select(p => (p, positions.GetValueOrDefault(p.Id), currentTeams.TryGetValue(p.Id, out var team) ? team : (int?)null))
            .ToList();
    }

    private async Task<Dictionary<int, int>> GetCurrentTeams(DateTime nowUtc, CancellationToken ct)
    {
        // Compared here, not in SQL: SnapshotUTC is a plain timestamp column
        var snapshot = await db.RosterStatus.AsNoTracking().MaxAsync(r => (DateTime?)r.SnapshotUTC, ct);
        if (snapshot != null && snapshot.Value >= nowUtc - RosterReportMaxAge)
        {
            return await db.RosterStatus.AsNoTracking()
                .Where(r => r.SnapshotUTC == snapshot.Value)
                .ToDictionaryAsync(r => r.PlayerId, r => r.TeamId, ct);
        }

        // No recent report: the team of each player's latest game this season (game ids start with the season)
        var first = NhlSeason.Current(nowUtc) * 1_000_000;
        var rows = await db.GameSkaterStats.AsNoTracking()
                .Where(s => s.GameId >= first)
                .Select(s => new { s.PlayerId, s.GameId, s.TeamId })
                .ToListAsync(ct);
        rows.AddRange(await db.GameGoalieStats.AsNoTracking()
                .Where(s => s.GameId >= first)
                .Select(s => new { s.PlayerId, s.GameId, s.TeamId })
                .ToListAsync(ct));
        return rows.GroupBy(r => r.PlayerId).ToDictionary(g => g.Key, g => g.MaxBy(r => r.GameId)!.TeamId);
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
