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
/// Current teams and this season's rosters come from the NHL's roster report (RosterStatus, a snapshot saved by
/// every NhlAdd run), since box scores can't tell a trade or a demotion until the player's next game (#170).
/// </summary>
public class WebPlayerRepository : IWebPlayerRepository
{
    // NhlAdd saves a roster report snapshot daily; one older than this means the job or the report is down,
    // and current teams fall back to the box scores
    public static readonly TimeSpan RosterReportMaxAge = TimeSpan.FromDays(3);

    private readonly GameDbContext _dbContext;
    private readonly TimeProvider _clock;

    public WebPlayerRepository(GameDbContext dbContext, TimeProvider? clock = null)
    {
        _dbContext = dbContext;
        _clock = clock ?? TimeProvider.System;
    }

    private int CurrentSeason => NhlSeason.Current(_clock.GetUtcNow().UtcDateTime);

    // The latest roster report snapshot's time, or null when there's none from the last RosterReportMaxAge.
    // Compared here, not in SQL: SnapshotUTC is a plain timestamp column
    private async Task<DateTime?> FreshRosterSnapshot()
    {
        var latest = await _dbContext.RosterStatus.AsNoTracking().MaxAsync(r => (DateTime?)r.SnapshotUTC);
        return latest != null && latest.Value >= _clock.GetUtcNow().UtcDateTime - RosterReportMaxAge ? latest : null;
    }

    private sealed record RosterReportRow(int PlayerId, int TeamId, bool IsInjuredReserve);

    private async Task<List<RosterReportRow>> RosterReportRows(DateTime snapshot, IReadOnlyCollection<int>? playerIds = null, int? teamId = null)
    {
        var rows = _dbContext.RosterStatus.AsNoTracking().Where(r => r.SnapshotUTC == snapshot);
        if (playerIds != null)
            rows = rows.Where(r => playerIds.Contains(r.PlayerId));
        if (teamId != null)
            rows = rows.Where(r => r.TeamId == teamId);
        return await rows.Select(r => new RosterReportRow(r.PlayerId, r.TeamId, r.IsInjuredReserve)).ToListAsync();
    }

    // The current team rule (PlayerProfile.CurrentTeam): the roster report when there's a recent one (not on it
    // means on no NHL roster), else the team of the latest game if it was this season
    private int? CurrentTeamId(bool freshReport, RosterReportRow? reportRow, PlayerGameRef? latest) =>
        freshReport ? reportRow?.TeamId
        : latest != null && SeasonOf(latest.GameId) == CurrentSeason ? latest.TeamId
        : null;

    // A team as it was named in a season (or the latest season before it: relocations change names)
    private async Task<Team?> SeasonTeam(int teamId, int seasonStartYear)
    {
        var team = await _dbContext.SeasonTeam
            .AsNoTracking()
            .Where(t => t.TeamId == teamId && t.SeasonStartYear <= seasonStartYear)
            .OrderByDescending(t => t.SeasonStartYear)
            .FirstOrDefaultAsync();
        return team != null ? DbSeasonTeamToTeamMapper.Map(team) : null;
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
        if (latest != null)
        {
            profile.Position = latest.Position;
            profile.SweaterNumber = await _dbContext.GameRosterSpot
                .AsNoTracking()
                .Where(r => r.GameId == latest.GameId && r.PlayerId == playerId)
                .Select(r => r.SweaterNumber)
                .FirstOrDefaultAsync();
            profile.LastSeasonStartYear = SeasonOf(latest.GameId);
            profile.LastTeam = await SeasonTeam(latest.TeamId, SeasonOf(latest.GameId));
        }

        var snapshot = await FreshRosterSnapshot();
        var reportRow = snapshot != null ? (await RosterReportRows(snapshot.Value, playerIds: [playerId])).FirstOrDefault() : null;
        var currentTeamId = CurrentTeamId(snapshot != null, reportRow, latest);
        if (currentTeamId != null)
        {
            profile.CurrentTeam = currentTeamId == profile.LastTeam?.Id && profile.LastSeasonStartYear == CurrentSeason
                ? profile.LastTeam
                : await SeasonTeam(currentTeamId.Value, CurrentSeason);
            profile.IsInjuredReserve = reportRow?.IsInjuredReserve ?? false;
        }
        return profile;
    }

    // Their most recent game in the box scores (game ids grow through a season and from season to season)
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
        if (seasonStartYear == CurrentSeason && await FreshRosterSnapshot() is { } snapshot)
            return await GetReportRoster(teamId, seasonStartYear, snapshot);
        return await GetPlayedRoster(teamId, seasonStartYear);
    }

    // This season with a recent roster report: the players it lists for the team (injured reserve included and
    // marked), with their games for the team this season (0 for one who hasn't played for them yet)
    private async Task<List<RosterPlayer>> GetReportRoster(int teamId, int seasonStartYear, DateTime snapshot)
    {
        var report = await RosterReportRows(snapshot, teamId: teamId);
        if (report.Count == 0)
            return new List<RosterPlayer>();
        var ids = report.Select(r => r.PlayerId).ToList();
        var first = FirstGameId(seasonStartYear);
        var next = FirstGameId(seasonStartYear + 1);

        var gamesForTeam = (await _dbContext.GameSkaterStats
                .AsNoTracking()
                .Where(s => ids.Contains(s.PlayerId) && s.TeamId == teamId && s.GameId >= first && s.GameId < next && s.TimeOnIceSeconds > 0)
                .Select(s => new { s.PlayerId, s.GameId })
                .ToListAsync())
            .Concat(await _dbContext.GameGoalieStats
                .AsNoTracking()
                .Where(s => ids.Contains(s.PlayerId) && s.TeamId == teamId && s.GameId >= first && s.GameId < next && s.TimeOnIceSeconds > 0)
                .Select(s => new { s.PlayerId, s.GameId })
                .ToListAsync())
            .GroupBy(x => x.PlayerId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GameId).Distinct().Count());

        // Position and number from each one's latest game, wherever it was (the report has neither). The report
        // only matches players with games, so everyone has one
        var latest = (await LatestGames(ids)).ToDictionary(r => r.PlayerId);
        var players = await _dbContext.Player
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.HeadShot })
            .ToDictionaryAsync(p => p.Id);
        var numbers = await GetSweaterNumbers(latest.Values.Select(r => (r.GameId, r.PlayerId)).ToList());

        return report
            .Where(r => players.ContainsKey(r.PlayerId) && latest.ContainsKey(r.PlayerId))
            .Select(r =>
            {
                var p = players[r.PlayerId];
                var game = latest[r.PlayerId];
                return new RosterPlayer
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    HeadShot = p.HeadShot ?? string.Empty,
                    TeamId = teamId,
                    Position = game.Position,
                    SweaterNumber = numbers.GetValueOrDefault((game.GameId, game.PlayerId)),
                    GamesPlayed = gamesForTeam.GetValueOrDefault(r.PlayerId),
                    IsInjuredReserve = r.IsInjuredReserve,
                };
            })
            .ToList();
    }

    // Past seasons, or this one without a recent roster report: the players whose latest game that season was
    // for the team
    private async Task<List<RosterPlayer>> GetPlayedRoster(int teamId, int seasonStartYear)
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
            // Injured reserve can't play, so a game's rosters leave them out
            result.Roster = (await GetTeamRoster(game.AwayTeamId, game.SeasonStartYear))
                .Concat(await GetTeamRoster(game.HomeTeamId, game.SeasonStartYear))
                .Where(p => !p.IsInjuredReserve)
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

    public Task<List<ActivePlayer>> GetActivePlayers(int seasonStartYear) =>
        GetLastGames(FirstGameId(seasonStartYear), FirstGameId(seasonStartYear + 1));

    public async Task<List<ActivePlayer>> GetPlayersLastGames()
    {
        // Only players with a page: a box score row for an id missing from Player would be a 404
        var withPage = (await _dbContext.Player.AsNoTracking().Select(p => p.Id).ToListAsync()).ToHashSet();
        var lastGames = await GetLastGames(0, int.MaxValue);
        return lastGames.Where(p => withPage.Contains(p.Id)).ToList();
    }

    // Each player with a box score row in [first, next) game ids, and when the latest of those games started.
    // Grouped in Postgres, so one row per player comes back, not one per game.
    private async Task<List<ActivePlayer>> GetLastGames(int first, int next)
    {
        var skaters = await (
            from s in _dbContext.GameSkaterStats.AsNoTracking()
            where s.GameId >= first && s.GameId < next
            join g in _dbContext.GameRaw.AsNoTracking() on s.GameId equals g.Id
            group g.GameDateUTC by s.PlayerId into grp
            select new { PlayerId = grp.Key, Last = grp.Max() }).ToListAsync();
        var goalies = await (
            from s in _dbContext.GameGoalieStats.AsNoTracking()
            // A backup who dressed but never went in didn't play (as on the Player page)
            where s.GameId >= first && s.GameId < next && s.TimeOnIceSeconds > 0
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
        var rows = await LatestGames(playerIds);

        var snapshot = await FreshRosterSnapshot();
        var report = snapshot != null
            ? (await RosterReportRows(snapshot.Value, playerIds: rows.Select(r => r.PlayerId).ToList())).ToDictionary(r => r.PlayerId)
            : new Dictionary<int, RosterReportRow>();
        var current = rows.ToDictionary(r => r.PlayerId, r => CurrentTeamId(snapshot != null, report.GetValueOrDefault(r.PlayerId), r));

        var teamIds = rows.Select(r => r.TeamId).Concat(current.Values.OfType<int>()).Distinct().ToList();
        var seasonTeams = await _dbContext.SeasonTeam
            .AsNoTracking()
            .Where(t => teamIds.Contains(t.TeamId))
            .Select(t => new { t.TeamId, t.SeasonStartYear, t.Abbreviation })
            .ToListAsync();
        // The team's abbreviation that season (or the latest before it: relocations change it)
        string? Abbreviation(int teamId, int season) => seasonTeams
            .Where(t => t.TeamId == teamId && t.SeasonStartYear <= season)
            .MaxBy(t => t.SeasonStartYear)?.Abbreviation;

        return rows.Select(r => new PlayerLatestTeam
        {
            PlayerId = r.PlayerId,
            Position = r.Position,
            TeamId = r.TeamId,
            TeamAbbreviation = Abbreviation(r.TeamId, SeasonOf(r.GameId)) ?? string.Empty,
            CurrentTeamId = current[r.PlayerId],
            CurrentTeamAbbreviation = current[r.PlayerId] is { } id ? Abbreviation(id, CurrentSeason) : null,
        }).ToList();
    }

    // Each player's latest box score row in any season; players without games are left out
    private async Task<List<PlayerGameRef>> LatestGames(IReadOnlyCollection<int> playerIds)
    {
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

        return (await _dbContext.GameSkaterStats
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

    // Regular season game ids: {season}02{number}
    private static int FirstRegularSeasonGameId(int seasonStartYear) => FirstGameId(seasonStartYear) + 20_000;

    public async Task<List<PlayerSeasonTotals>> GetSeasonTotals(int seasonStartYear)
    {
        var first = FirstRegularSeasonGameId(seasonStartYear);
        var next = first + 10_000;

        // Summed in the database: one row per player rather than one per game
        var skaters = await _dbContext.GameSkaterStats
            .AsNoTracking()
            .Where(s => s.GameId >= first && s.GameId < next)
            .GroupBy(s => s.PlayerId)
            .Select(g => new
            {
                PlayerId = g.Key,
                GamesPlayed = g.Sum(s => s.TimeOnIceSeconds > 0 ? 1 : 0),
                TimeOnIceSeconds = g.Sum(s => s.TimeOnIceSeconds),
                Goals = g.Sum(s => s.Goals),
                Assists = g.Sum(s => s.Assists),
                Shots = g.Sum(s => s.ShotsOnGoal),
                PlusMinus = g.Sum(s => s.PlusMinus),
                PenaltyMinutes = g.Sum(s => s.PenaltyMinutes),
                PowerPlayGoals = g.Sum(s => s.PowerPlayGoals),
                Hits = g.Sum(s => s.Hits),
                BlockedShots = g.Sum(s => s.BlockedShots),
                LatestGameId = g.Max(s => s.GameId),
            })
            .ToListAsync();

        // A few thousand rows a season: shutouts need each game's goalies, so these are summed here
        var goalieGames = await _dbContext.GameGoalieStats
            .AsNoTracking()
            .Where(s => s.GameId >= first && s.GameId < next)
            .Select(s => new
            {
                s.PlayerId,
                s.GameId,
                s.TeamId,
                s.TimeOnIceSeconds,
                s.IsStarter,
                Saves = s.EvenStrengthShotsSaved + s.PowerPlayShotsSaved + s.ShortHandedShotsSaved,
                GoalsAgainst = s.EvenStrengthGoalsAllowed + s.PowerPlayGoalsAllowed + s.ShortHandedGoalsAllowed,
            })
            .ToListAsync();
        var goaliesPerTeamGame = goalieGames
            .Where(s => s.TimeOnIceSeconds > 0)
            .GroupBy(s => (s.GameId, s.TeamId))
            .ToDictionary(g => g.Key, g => g.Count());
        var goalies = goalieGames
            .GroupBy(s => s.PlayerId)
            .Select(g => new
            {
                PlayerId = g.Key,
                GamesPlayed = g.Count(s => s.TimeOnIceSeconds > 0),
                TimeOnIceSeconds = g.Sum(s => s.TimeOnIceSeconds),
                GamesStarted = g.Count(s => s.IsStarter),
                Saves = g.Sum(s => s.Saves),
                GoalsAgainst = g.Sum(s => s.GoalsAgainst),
                Shutouts = g.Count(s => s.TimeOnIceSeconds > 0 && s.GoalsAgainst == 0
                    && goaliesPerTeamGame.GetValueOrDefault((s.GameId, s.TeamId)) == 1),
                LatestGameId = g.Max(s => s.GameId),
            })
            .ToList();

        // Team and position from each player's latest game
        var latestGames = skaters.Select(s => s.LatestGameId).Concat(goalies.Select(g => g.LatestGameId)).Distinct().ToList();
        var skaterIds = skaters.Select(s => s.PlayerId).ToList();
        var latestSkater = (await _dbContext.GameSkaterStats
                .AsNoTracking()
                .Where(s => latestGames.Contains(s.GameId) && skaterIds.Contains(s.PlayerId))
                .Select(s => new { s.PlayerId, s.GameId, s.TeamId, s.Position })
                .ToListAsync())
            .ToDictionary(s => (s.PlayerId, s.GameId));
        var latestGoalieTeam = goalieGames.ToDictionary(s => (s.PlayerId, s.GameId), s => s.TeamId);

        var ids = skaterIds.Concat(goalies.Select(g => g.PlayerId)).Distinct().ToList();
        var players = await _dbContext.Player
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.HeadShot })
            .ToDictionaryAsync(p => p.Id);
        var teams = await GetSeasonTeams(seasonStartYear);

        PlayerSeasonTotals Totals(int playerId, int teamId, POSITION position)
        {
            var p = players[playerId];
            var team = teams.GetValueOrDefault(teamId);
            return new PlayerSeasonTotals
            {
                PlayerId = playerId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                HeadShot = p.HeadShot ?? string.Empty,
                Position = position,
                IsGoalie = position == POSITION.Goalie,
                TeamId = teamId,
                TeamAbbreviation = team?.Abbreviation ?? string.Empty,
                TeamName = team?.Name ?? string.Empty,
                TeamLogoUri = team?.LogoUri ?? string.Empty,
            };
        }

        var result = new List<PlayerSeasonTotals>();
        foreach (var s in skaters.Where(s => players.ContainsKey(s.PlayerId)))
        {
            var latest = latestSkater.GetValueOrDefault((s.PlayerId, s.LatestGameId));
            var t = Totals(s.PlayerId, latest?.TeamId ?? 0, latest?.Position ?? POSITION.Center);
            t.GamesPlayed = s.GamesPlayed;
            t.TimeOnIceSeconds = s.TimeOnIceSeconds;
            t.Goals = s.Goals;
            t.Assists = s.Assists;
            t.Shots = s.Shots;
            t.PlusMinus = s.PlusMinus;
            t.PenaltyMinutes = s.PenaltyMinutes;
            t.PowerPlayGoals = s.PowerPlayGoals;
            t.Hits = s.Hits;
            t.BlockedShots = s.BlockedShots;
            result.Add(t);
        }
        foreach (var g in goalies.Where(g => players.ContainsKey(g.PlayerId)))
        {
            var t = Totals(g.PlayerId, latestGoalieTeam.GetValueOrDefault((g.PlayerId, g.LatestGameId)), POSITION.Goalie);
            t.GamesPlayed = g.GamesPlayed;
            t.TimeOnIceSeconds = g.TimeOnIceSeconds;
            t.GamesStarted = g.GamesStarted;
            t.Saves = g.Saves;
            t.GoalsAgainst = g.GoalsAgainst;
            t.Shutouts = g.Shutouts;
            result.Add(t);
        }
        return result;
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
