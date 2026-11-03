using Entities.Models.Web;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebLineupRepository;

public class WebLineupRepository(GameDbContext dbContext) : IWebLineupRepository
{
    public async Task<GameLineup?> GetGameLineup(int gameId)
    {
        var start = await dbContext.GameRaw.AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => (DateTime?)g.GameDateUTC)
            .FirstOrDefaultAsync();
        if (start == null)
            return null;

        // Versions seen after puck drop aren't projections for this game any more
        var versions = await dbContext.LineupGame.AsNoTracking()
            .Where(s => s.GameId == gameId && s.FirstSeenUTC < start)
            .OrderByDescending(s => s.FirstSeenUTC)
            .Select(s => new { s.SectionHash, s.FirstSeenUTC, s.StatusReport })
            .ToListAsync();
        if (versions.Count == 0)
            return null;

        var latest = versions[0];
        var players = await dbContext.LineupPlayer.AsNoTracking()
            .Where(p => p.SectionHash == latest.SectionHash)
            .Select(p => new GameLineupPlayer(p.Side, p.Group, p.LineNumber, p.Slot, p.Name, p.PlayerId, p.Note))
            .ToListAsync();

        return new GameLineup
        {
            GameId = gameId,
            UpdatedUTC = DateTime.SpecifyKind(latest.FirstSeenUTC, DateTimeKind.Utc),
            VersionCount = versions.Count,
            StatusReport = latest.StatusReport,
            Players = players,
        };
    }
}
