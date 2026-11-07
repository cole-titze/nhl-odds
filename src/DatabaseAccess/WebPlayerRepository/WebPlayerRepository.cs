using DatabaseAccess.WebPlayerRepository.Mappers;
using DatabaseAccess.WebTeamRepository.Mappers;
using Entities.DbModels;
using Entities.Models.Web;
using Entities.Types;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebPlayerRepository;

/// <summary>
/// Players for the web API, from the box score tables (GameSkaterStats, GameGoalieStats: one row per player
/// per played game, every season) and GameRosterSpot (sweater numbers, where fetched). A season's games are
/// read by game id range (the id starts with the season's start year), which the tables' primary keys cover.
/// </summary>
public class WebPlayerRepository : IWebPlayerRepository
{
    private readonly GameDbContext _dbContext;

    public WebPlayerRepository(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static int FirstGameId(int seasonStartYear) => seasonStartYear * 1_000_000;
    private static int SeasonOf(int gameId) => gameId / 1_000_000;

    private sealed record PlayerGameRef(int PlayerId, int GameId, int TeamId, POSITION Position, int TimeOnIceSeconds);

    public async Task<PlayerProfile?> GetPlayer(int playerId)
    {
        var player = await _dbContext.Player
            .AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.HeadShot,
                BirthDate = (DateTime?)p.BirthDate,
                p.BirthCity,
                p.BirthStateProvince,
                p.HeightInInches,
                p.WeightInPounds,
            })
            .FirstOrDefaultAsync();
        if (player == null)
            return null;

        var profile = new PlayerProfile
        {
            Id = player.Id,
            FirstName = player.FirstName,
            LastName = player.LastName,
            HeadShot = player.HeadShot ?? string.Empty,
            BirthDate = player.BirthDate,
            BirthCity = player.BirthCity ?? string.Empty,
            BirthStateProvince = player.BirthStateProvince ?? string.Empty,
            HeightInInches = player.HeightInInches,
            WeightInPounds = player.WeightInPounds,
        };

        var latest = await GetLatestGame(playerId);
        if (latest == null)
            return profile;

        profile.Position = latest.Position;
        profile.SweaterNumber = await _dbContext.GameRosterSpot
            .AsNoTracking()
            .Where(r => r.GameId == latest.GameId && r.PlayerId == playerId)
            .Select(r => r.SweaterNumber)
            .FirstOrDefaultAsync();
        var team = await _dbContext.SeasonTeam
            .AsNoTracking()
            .Where(t => t.TeamId == latest.TeamId && t.SeasonStartYear <= SeasonOf(latest.GameId))
            .OrderByDescending(t => t.SeasonStartYear)
            .FirstOrDefaultAsync();
        profile.CurrentTeam = team != null ? DbSeasonTeamToTeamMapper.Map(team) : null;
        return profile;
    }

    // His most recent game in the box scores (game ids grow through a season and from season to season)
    private async Task<PlayerGameRef?> GetLatestGame(int playerId)
    {
        var skater = await _dbContext.GameSkaterStats
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.GameId)
            .Select(s => new PlayerGameRef(s.PlayerId, s.GameId, s.TeamId, s.Position, s.TimeOnIceSeconds))
            .FirstOrDefaultAsync();
        var goalie = await _dbContext.GameGoalieStats
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.GameId)
            .Select(s => new PlayerGameRef(s.PlayerId, s.GameId, s.TeamId, POSITION.Goalie, s.TimeOnIceSeconds))
            .FirstOrDefaultAsync();
        if (skater == null)
            return goalie;
        if (goalie == null)
            return skater;
        return goalie.GameId > skater.GameId ? goalie : skater;
    }

    public async Task<List<PlayerGameLine>> GetPlayerGames(int playerId, int seasonStartYear)
    {
        var first = FirstGameId(seasonStartYear);
        var next = FirstGameId(seasonStartYear + 1);

        var skaterRows = await (
            from s in _dbContext.GameSkaterStats.AsNoTracking()
            where s.PlayerId == playerId && s.GameId >= first && s.GameId < next
            join g in _dbContext.GameRaw.AsNoTracking() on s.GameId equals g.Id
            select new
            {
                Stats = s,
                Game = new GameRow(g.Id, g.GameDateUTC, g.SeasonStartYear, g.HomeTeamId, g.AwayTeamId,
                    g.HomeGoals, g.AwayGoals, g.Winner, g.EndPeriod, g.HasBeenPlayed, g.GameType),
            }).ToListAsync();
        var goalieRows = await (
            from s in _dbContext.GameGoalieStats.AsNoTracking()
            // A backup who dressed but never went in didn't play
            where s.PlayerId == playerId && s.GameId >= first && s.GameId < next && s.TimeOnIceSeconds > 0
            join g in _dbContext.GameRaw.AsNoTracking() on s.GameId equals g.Id
            select new
            {
                Stats = s,
                Game = new GameRow(g.Id, g.GameDateUTC, g.SeasonStartYear, g.HomeTeamId, g.AwayTeamId,
                    g.HomeGoals, g.AwayGoals, g.Winner, g.EndPeriod, g.HasBeenPlayed, g.GameType),
            }).ToListAsync();

        var seasonTeams = await GetSeasonTeams(seasonStartYear);
        var lines = skaterRows.Select(r => SkaterLine(r.Stats, GameRowToGameMapper.Map(r.Game, seasonTeams)))
            .Concat(goalieRows.Select(r => GoalieLine(r.Stats, GameRowToGameMapper.Map(r.Game, seasonTeams))))
            .OrderByDescending(l => l.Game.GameDate)
            .ThenByDescending(l => l.Game.Id)
            .ToList();
        return lines;
    }

    public async Task<List<RosterPlayer>> GetTeamRoster(int teamId, int seasonStartYear)
    {
        var first = FirstGameId(seasonStartYear);
        var next = FirstGameId(seasonStartYear + 1);

        // Everyone who played for the team this season
        var forTeam = await _dbContext.GameSkaterStats
            .AsNoTracking()
            .Where(s => s.TeamId == teamId && s.GameId >= first && s.GameId < next)
            .Select(s => new PlayerGameRef(s.PlayerId, s.GameId, s.TeamId, s.Position, s.TimeOnIceSeconds))
            .ToListAsync();
        forTeam.AddRange(await _dbContext.GameGoalieStats
            .AsNoTracking()
            .Where(s => s.TeamId == teamId && s.GameId >= first && s.GameId < next)
            .Select(s => new PlayerGameRef(s.PlayerId, s.GameId, s.TeamId, POSITION.Goalie, s.TimeOnIceSeconds))
            .ToListAsync());
        if (forTeam.Count == 0)
            return new List<RosterPlayer>();

        var playerIds = forTeam.Select(r => r.PlayerId).Distinct().ToList();

        // ...less those whose latest game this season was for another team
        var latestAny = (await _dbContext.GameSkaterStats
                .AsNoTracking()
                .Where(s => playerIds.Contains(s.PlayerId) && s.GameId >= first && s.GameId < next)
                .GroupBy(s => s.PlayerId)
                .Select(g => new { PlayerId = g.Key, GameId = g.Max(s => s.GameId) })
                .ToListAsync())
            .Concat(await _dbContext.GameGoalieStats
                .AsNoTracking()
                .Where(s => playerIds.Contains(s.PlayerId) && s.GameId >= first && s.GameId < next)
                .GroupBy(s => s.PlayerId)
                .Select(g => new { PlayerId = g.Key, GameId = g.Max(s => s.GameId) })
                .ToListAsync())
            .GroupBy(x => x.PlayerId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.GameId));

        var current = forTeam
            .GroupBy(r => r.PlayerId)
            .Select(g => new { Latest = g.MaxBy(r => r.GameId)!, GamesPlayed = g.Where(r => r.TimeOnIceSeconds > 0).Select(r => r.GameId).Distinct().Count() })
            .Where(x => !latestAny.TryGetValue(x.Latest.PlayerId, out var latestGameId) || latestGameId == x.Latest.GameId)
            .ToList();
        var currentIds = current.Select(x => x.Latest.PlayerId).ToList();

        var players = await _dbContext.Player
            .AsNoTracking()
            .Where(p => currentIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.HeadShot })
            .ToDictionaryAsync(p => p.Id);
        var numbers = await GetSweaterNumbers(current.Select(x => (x.Latest.GameId, x.Latest.PlayerId)).ToList());

        return current
            .Where(x => players.ContainsKey(x.Latest.PlayerId))
            .Select(x =>
            {
                var p = players[x.Latest.PlayerId];
                return new RosterPlayer
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    HeadShot = p.HeadShot ?? string.Empty,
                    TeamId = teamId,
                    Position = x.Latest.Position,
                    SweaterNumber = numbers.GetValueOrDefault((x.Latest.GameId, x.Latest.PlayerId)),
                    GamesPlayed = x.GamesPlayed,
                };
            })
            .ToList();
    }

    public async Task<GamePlayers?> GetGamePlayers(int gameId)
    {
        var game = await _dbContext.GameRaw
            .AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => new GameRow(g.Id, g.GameDateUTC, g.SeasonStartYear, g.HomeTeamId, g.AwayTeamId,
                g.HomeGoals, g.AwayGoals, g.Winner, g.EndPeriod, g.HasBeenPlayed, g.GameType))
            .FirstOrDefaultAsync();
        if (game == null)
            return null;

        var seasonTeams = await GetSeasonTeams(game.SeasonStartYear);
        var result = new GamePlayers { Game = GameRowToGameMapper.Map(game, seasonTeams) };

        if (game.HasBeenPlayed)
        {
            var skaters = await _dbContext.GameSkaterStats.AsNoTracking().Where(s => s.GameId == gameId).ToListAsync();
            var goalies = await _dbContext.GameGoalieStats.AsNoTracking().Where(s => s.GameId == gameId).ToListAsync();
            result.Lines = skaters.Select(s => SkaterLine(s, result.Game))
                .Concat(goalies.Select(s => GoalieLine(s, result.Game)))
                .ToList();
        }

        if (result.Lines.Count == 0)
        {
            result.Roster = (await GetTeamRoster(game.AwayTeamId, game.SeasonStartYear))
                .Concat(await GetTeamRoster(game.HomeTeamId, game.SeasonStartYear))
                .ToList();
            return result;
        }

        var ids = result.Lines.Select(l => l.PlayerId).ToList();
        var names = await _dbContext.Player
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName })
            .ToDictionaryAsync(p => p.Id);
        var numbers = await _dbContext.GameRosterSpot
            .AsNoTracking()
            .Where(r => r.GameId == gameId)
            .ToDictionaryAsync(r => r.PlayerId, r => r.SweaterNumber);
        foreach (var line in result.Lines)
        {
            if (names.TryGetValue(line.PlayerId, out var name))
            {
                line.FirstName = name.FirstName;
                line.LastName = name.LastName;
            }
            line.SweaterNumber = numbers.GetValueOrDefault(line.PlayerId);
        }
        return result;
    }

    public async Task<List<ActivePlayer>> GetActivePlayers(int seasonStartYear)
    {
        var first = FirstGameId(seasonStartYear);
        var next = FirstGameId(seasonStartYear + 1);

        var skaters = await (
            from s in _dbContext.GameSkaterStats.AsNoTracking()
            where s.GameId >= first && s.GameId < next
            join g in _dbContext.GameRaw.AsNoTracking() on s.GameId equals g.Id
            group g.GameDateUTC by s.PlayerId into grp
            select new { PlayerId = grp.Key, Last = grp.Max() }).ToListAsync();
        var goalies = await (
            from s in _dbContext.GameGoalieStats.AsNoTracking()
            where s.GameId >= first && s.GameId < next
            join g in _dbContext.GameRaw.AsNoTracking() on s.GameId equals g.Id
            group g.GameDateUTC by s.PlayerId into grp
            select new { PlayerId = grp.Key, Last = grp.Max() }).ToListAsync();

        return skaters.Concat(goalies)
            .GroupBy(x => x.PlayerId)
            .Select(g => new ActivePlayer
            {
                Id = g.Key,
                LastGameDateUTC = DateTime.SpecifyKind(g.Max(x => x.Last), DateTimeKind.Utc),
            })
            .OrderBy(p => p.Id)
            .ToList();
    }

    public async Task<List<PlayerName>> GetPlayerNames()
    {
        return await _dbContext.Player
            .AsNoTracking()
            .Select(p => new PlayerName { Id = p.Id, FirstName = p.FirstName, LastName = p.LastName })
            .ToListAsync();
    }

    public async Task<List<PlayerLatestTeam>> GetLatestTeams(IReadOnlyCollection<int> playerIds)
    {
        if (playerIds.Count == 0)
            return new List<PlayerLatestTeam>();
        var ids = playerIds.Distinct().ToList();

        var skaterLatest = await _dbContext.GameSkaterStats
            .AsNoTracking()
            .Where(s => ids.Contains(s.PlayerId))
            .GroupBy(s => s.PlayerId)
            .Select(g => new { PlayerId = g.Key, GameId = g.Max(s => s.GameId) })
            .ToListAsync();
        var goalieLatest = await _dbContext.GameGoalieStats
            .AsNoTracking()
            .Where(s => ids.Contains(s.PlayerId))
            .GroupBy(s => s.PlayerId)
            .Select(g => new { PlayerId = g.Key, GameId = g.Max(s => s.GameId) })
            .ToListAsync();
        var skaterGames = skaterLatest.Select(x => x.GameId).ToList();
        var goalieGames = goalieLatest.Select(x => x.GameId).ToList();

        var rows = (await _dbContext.GameSkaterStats
                .AsNoTracking()
                .Where(s => ids.Contains(s.PlayerId) && skaterGames.Contains(s.GameId))
                .Select(s => new PlayerGameRef(s.PlayerId, s.GameId, s.TeamId, s.Position, s.TimeOnIceSeconds))
                .ToListAsync())
            .Concat(await _dbContext.GameGoalieStats
                .AsNoTracking()
                .Where(s => ids.Contains(s.PlayerId) && goalieGames.Contains(s.GameId))
                .Select(s => new PlayerGameRef(s.PlayerId, s.GameId, s.TeamId, POSITION.Goalie, s.TimeOnIceSeconds))
                .ToListAsync())
            .GroupBy(r => r.PlayerId)
            .Select(g => g.MaxBy(r => r.GameId)!)
            .ToList();

        var teamIds = rows.Select(r => r.TeamId).Distinct().ToList();
        var seasonTeams = await _dbContext.SeasonTeam
            .AsNoTracking()
            .Where(t => teamIds.Contains(t.TeamId))
            .Select(t => new { t.TeamId, t.SeasonStartYear, t.Abbreviation })
            .ToListAsync();

        return rows.Select(r => new PlayerLatestTeam
        {
            PlayerId = r.PlayerId,
            Position = r.Position,
            TeamId = r.TeamId,
            // The team's abbreviation that season (or the latest before it: relocations change it)
            TeamAbbreviation = seasonTeams
                .Where(t => t.TeamId == r.TeamId && t.SeasonStartYear <= SeasonOf(r.GameId))
                .MaxBy(t => t.SeasonStartYear)?.Abbreviation ?? string.Empty,
        }).ToList();
    }

    private async Task<Dictionary<(int GameId, int PlayerId), short?>> GetSweaterNumbers(List<(int GameId, int PlayerId)> keys)
    {
        if (keys.Count == 0)
            return new Dictionary<(int, int), short?>();
        var gameIds = keys.Select(k => k.GameId).Distinct().ToList();
        var playerIds = keys.Select(k => k.PlayerId).Distinct().ToList();
        var wanted = keys.ToHashSet();
        var spots = await _dbContext.GameRosterSpot
            .AsNoTracking()
            .Where(r => gameIds.Contains(r.GameId) && playerIds.Contains(r.PlayerId))
            .Select(r => new { r.GameId, r.PlayerId, r.SweaterNumber })
            .ToListAsync();
        return spots
            .Where(r => wanted.Contains((r.GameId, r.PlayerId)))
            .ToDictionary(r => (r.GameId, r.PlayerId), r => r.SweaterNumber);
    }

    private async Task<Dictionary<int, DbSeasonTeam>> GetSeasonTeams(int seasonStartYear)
    {
        return await _dbContext.SeasonTeam
            .AsNoTracking()
            .Where(x => x.SeasonStartYear == seasonStartYear)
            .ToDictionaryAsync(t => t.TeamId);
    }

    private static PlayerGameLine SkaterLine(DbGameSkaterStats s, Game game) => new()
    {
        PlayerId = s.PlayerId,
        TeamId = s.TeamId,
        Position = s.Position,
        Game = game,
        TimeOnIceSeconds = s.TimeOnIceSeconds,
        Goals = s.Goals,
        Assists = s.Assists,
        ShotsOnGoal = s.ShotsOnGoal,
        PlusMinus = s.PlusMinus,
    };

    private static PlayerGameLine GoalieLine(DbGameGoalieStats s, Game game)
    {
        var saves = s.EvenStrengthShotsSaved + s.PowerPlayShotsSaved + s.ShortHandedShotsSaved;
        var goalsAgainst = s.EvenStrengthGoalsAllowed + s.PowerPlayGoalsAllowed + s.ShortHandedGoalsAllowed;
        return new PlayerGameLine
        {
            PlayerId = s.PlayerId,
            TeamId = s.TeamId,
            Position = POSITION.Goalie,
            Game = game,
            TimeOnIceSeconds = s.TimeOnIceSeconds,
            Saves = saves,
            GoalsAgainst = goalsAgainst,
            ShotsAgainst = saves + goalsAgainst,
            IsStarter = s.IsStarter,
        };
    }
}
