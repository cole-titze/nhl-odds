using DatabaseAccess.BroadcasterRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameEventRepository;
using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using DatabaseAccess.TeamRepository;
using Entities.DbModels;
using Entities.Models;
using Entities.Models.Teams;
using Entities.Types;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;

using Services.NhlData;

namespace DataGetter.BusinessLogic;

public class NhlDataManager
{
    private readonly IGameRepository _gameRepo;
    private readonly IPlayerRepository _playerRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly IErrorRepository _errorRepo;
    private readonly IBroadcasterRepository _broadcasterRepo;
    private readonly IGameEventRepository _gameEventRepo;
    private readonly NhlTeamManager _teamManager;
    private readonly NhlGameManager _gameManager;
    private readonly NhlPlayerManager _playerManager;
    private readonly ILogger<NhlDataManager> _logger;
    public NhlDataManager(IGameRepository gameRepository, IPlayerRepository playerRepository, ITeamRepository teamRepository, IErrorRepository errorRepository, IBroadcasterRepository broadcasterRepository, IGameEventRepository gameEventRepository, NhlGameManager gameManager, NhlPlayerManager playerManager, NhlTeamManager teamManager, ILoggerFactory loggerFactory)
    {
        _gameRepo = gameRepository;
        _playerRepo = playerRepository;
        _teamRepo = teamRepository;
        _errorRepo = errorRepository;
        _broadcasterRepo = broadcasterRepository;
        _gameEventRepo = gameEventRepository;
        _gameManager = gameManager;
        _teamManager = teamManager;
        _playerManager = playerManager;
        _logger = loggerFactory.CreateLogger<NhlDataManager>();
    }

    /// <summary>
    /// Builds an error message that includes the innermost exception's detail (e.g. the actual
    /// Postgres constraint error inside a DbUpdateException, which ex.Message alone doesn't show).
    /// Bounded so a pathological exception chain can't write an unbounded blob into ErrorLog.
    /// </summary>
    private static string BuildErrorMessage(Exception ex, int maxInnerLength = 1000)
    {
        var inner = ex.InnerException;
        var depth = 0;
        while (inner?.InnerException != null && depth < 5)
        {
            inner = inner.InnerException;
            depth++;
        }

        if (inner == null)
            return ex.Message;

        var innerDetail = $"{inner.GetType().Name}: {inner.Message}";
        if (innerDetail.Length > maxInnerLength)
            innerDetail = innerDetail[..maxInnerLength];

        return $"{ex.Message} | Inner: {innerDetail}";
    }

    /// <summary>
    /// Force-fetches and overwrites a specific list of games by ID, regardless of whether they
    /// already exist. Useful for backfilling games that were saved with missing events or stats.
    /// </summary>
    public async Task BackfillGames(IEnumerable<int> gameIds)
    {
        foreach (var gameId in gameIds)
        {
            try
            {
                _logger.LogInformation("Backfilling game {GameId}", gameId);
                var game = await _gameManager.GetGame(gameId, ModeType.NhlUpdate);
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found in NHL API, skipping.", gameId);
                    continue;
                }
                // Skip SavePlayers — player rows already exist from the original save.
                // Re-saving them triggers FK violations via cascade-tracked entities from other games.
                await SaveGame(game);
                await _gameRepo.Commit();
                _logger.LogInformation("Backfilled game {GameId} successfully.", gameId);
            }
            catch (Exception ex)
            {
                _gameRepo.ClearTracking();
                _logger.LogError(ex, "Error backfilling game {GameId}.", gameId);
            }
        }
    }

    /// <summary>
    /// Re-fetches the shorthanded shots and goals of goalies in games saved before the mapper read them, one boxscore
    /// request per game, and updates only those fields. Re-runnable: games that already have them are skipped.
    /// </summary>
    /// <param name="seasonYearRange">The seasons to backfill</param>
    public async Task BackfillGoalieShortHandedStats(YearRange seasonYearRange)
    {
        for (int seasonStartYear = seasonYearRange.StartYear; seasonStartYear <= seasonYearRange.EndYear; seasonStartYear++)
        {
            var gameIds = await _playerRepo.GetGameIdsWithoutGoalieShortHandedStats(seasonStartYear);
            _logger.LogInformation("Backfilling goalie shorthanded stats for {Count} game(s) in season {Season}", gameIds.Count, seasonStartYear);
            int updatedGames = 0, failedGames = 0;
            foreach (var gameId in gameIds)
            {
                try
                {
                    var goalieStats = await _gameManager.GetGameGoalieStats(gameId);
                    if (goalieStats == null)
                    {
                        failedGames++;
                        continue;
                    }
                    if (await _playerRepo.UpdateGoalieShortHandedStats(gameId, goalieStats) > 0)
                        updatedGames++;
                    await _playerRepo.Commit();
                }
                catch (Exception ex)
                {
                    _gameRepo.ClearTracking();
                    failedGames++;
                    _logger.LogError(ex, "Error backfilling goalie stats for game {GameId}.", gameId);
                }
            }
            _logger.LogInformation("Season {Season}: updated {Updated} game(s), {Failed} failed", seasonStartYear, updatedGames, failedGames);
        }
    }

    /// <summary>
    /// Gets all nhl games within the season range.
    /// </summary>
    /// <param name="seasonYearRange">The years to get data for</param>
    /// <param name="mode">Whether to only add new players or also update existing players</param>
    public async Task GetNhlData(YearRange seasonYearRange, ModeType mode)
    {
        for (int seasonStartYear = seasonYearRange.StartYear; seasonStartYear <= seasonYearRange.EndYear; seasonStartYear++)
        {
            var isCurrentYear = seasonStartYear == seasonYearRange.EndYear;
            var hasAllSeasonGames = await HasAllSeasonGames(seasonStartYear);
            if (CanSkipSeason(mode, isCurrentYear, hasAllSeasonGames))
            {
                _logger.LogInformation("All game data for season " + seasonStartYear.ToString() + " already exists. Skipping...");
                continue;
            }

            await FetchAndSaveSeasonData(seasonStartYear, mode);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="seasonStartYear"></param>
    /// <param name="mode"></param>
    private async Task FetchAndSaveSeasonData(int seasonStartYear, ModeType mode)
    {
        var seasonTeams = await _teamManager.GetTeamData(seasonStartYear, mode);
        await SaveTeamData(seasonTeams);

        var seasonGameCount = await _gameManager.GetSeasonGameCount(seasonStartYear, mode);
        await SaveSeasonSchedule(seasonStartYear, seasonGameCount);

        await FetchSegment(seasonStartYear, mode,
            n => NhlApiDataGetter.GetGameId(seasonStartYear, n), seasonGameCount);

        // Playoff game IDs use NHL's RSMG format (Round/Series/Game), not sequential numbers.
        // Enumerate all valid combinations; the API returns null for games not yet played.
        await FetchPlayoffSegment(seasonStartYear, mode,
            NhlApiDataGetter.GetAllPlayoffGameIds(seasonStartYear));
    }

    /// <summary>
    /// Fetches a contiguous segment of games (regular season or playoffs).
    /// Iterates game numbers starting at 1; stops at <paramref name="knownCount"/> if provided,
    /// otherwise stops once <c>maxConsecutiveUnavailable</c> sequential games come back null.
    /// </summary>
    private async Task FetchSegment(int seasonStartYear, ModeType mode, Func<int, int> idForCount, int? knownCount)
    {
        const int maxConsecutiveUnavailable = 20;
        int consecutiveUnavailable = 0;

        int count = 1;
        while (knownCount == null || count <= knownCount.Value)
        {
            int gameId = idForCount(count);
            try
            {
                if (mode != ModeType.NhlUpdate && await _gameRepo.IsGamePlayed(gameId))
                {
                    consecutiveUnavailable = 0;
                    count++;
                    continue;
                }

                if (mode != ModeType.NhlUpdate && await _gameRepo.IsUnplayedFutureGame(gameId))
                {
                    _logger.LogInformation("Game {GameId} is a future game already saved. Skipping.", gameId);
                    count++;
                    continue;
                }

                var game = await _gameManager.GetGame(gameId, mode);
                if (game == null)
                {
                    consecutiveUnavailable++;
                    if (consecutiveUnavailable >= maxConsecutiveUnavailable)
                    {
                        _logger.LogInformation("Reached {Count} consecutive unavailable games after game {GameId}. Stopping segment for season {Season}.", maxConsecutiveUnavailable, gameId, seasonStartYear);
                        break;
                    }
                    count++;
                    continue;
                }

                consecutiveUnavailable = 0;

                var players = await _playerManager.GetPlayers(game, mode);

                await SavePlayers(players);
                await SaveGame(game);
                await _gameRepo.Commit();
            }
            catch (Exception ex)
            {
                _gameRepo.ClearTracking();
                _logger.LogError(ex, "Error processing game {GameId} in season {Season}. Skipping.", gameId, seasonStartYear);
                var stackTrace = ex.StackTrace ?? string.Empty;
                var stackFrames = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var topFrames = string.Join(" | ", stackFrames.Take(10).Select(f => f.Trim()));
                var errorLog = new DbErrorLog
                {
                    TimestampUTC = DateTime.UtcNow,
                    GameId = gameId,
                    SeasonStartYear = seasonStartYear,
                    ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                    Message = BuildErrorMessage(ex),
                    StackTrace = topFrames,
                    Source = "FetchSegment"
                };
                await _errorRepo.AddError(errorLog);
            }

            count++;
        }
    }

    /// <summary>
    /// Fetches a predetermined set of playoff game IDs. Unlike FetchSegment, there is no
    /// consecutive-miss cutoff — gaps between rounds and series are expected.
    /// </summary>
    private async Task FetchPlayoffSegment(int seasonStartYear, ModeType mode, IEnumerable<int> gameIds)
    {
        foreach (int gameId in gameIds)
        {
            try
            {
                if (mode != ModeType.NhlUpdate && await _gameRepo.IsGamePlayed(gameId))
                    continue;

                if (mode != ModeType.NhlUpdate && await _gameRepo.IsUnplayedFutureGame(gameId))
                {
                    _logger.LogInformation("Game {GameId} is a future game already saved. Skipping.", gameId);
                    continue;
                }

                var game = await _gameManager.GetGame(gameId, mode);
                if (game == null)
                {
                    var existing = await _gameRepo.GetGameSummary(gameId);
                    if (existing != null && !existing.HasBeenPlayed && existing.GameDateUTC < DateTime.UtcNow)
                    {
                        // This game slot was scheduled speculatively (e.g. Game 6 of a series that ended
                        // in 5) and its date has passed with the NHL API still returning nothing for it -
                        // it will never happen. Remove the placeholder instead of re-checking it forever.
                        _logger.LogInformation("Game {GameId} (scheduled {Date:yyyy-MM-dd}) never happened - series ended early. Removing placeholder.", gameId, existing.GameDateUTC);
                        await _gameRepo.DeleteGame(gameId);
                        await _gameRepo.Commit();
                    }
                    continue;
                }

                var players = await _playerManager.GetPlayers(game, mode);
                await SavePlayers(players);
                await SaveGame(game);
                await _gameRepo.Commit();
            }
            catch (Exception ex)
            {
                _gameRepo.ClearTracking();
                _logger.LogError(ex, "Error processing game {GameId} in season {Season}. Skipping.", gameId, seasonStartYear);
                var stackTrace = ex.StackTrace ?? string.Empty;
                var stackFrames = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var topFrames = string.Join(" | ", stackFrames.Take(10).Select(f => f.Trim()));
                var errorLog = new DbErrorLog
                {
                    TimestampUTC = DateTime.UtcNow,
                    GameId = gameId,
                    SeasonStartYear = seasonStartYear,
                    ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                    Message = BuildErrorMessage(ex),
                    StackTrace = topFrames,
                    Source = "FetchPlayoffSegment"
                };
                await _errorRepo.AddError(errorLog);
            }
        }
    }

    /// <summary>
    /// Saves the team data to the database
    /// </summary>
    /// <param name="teams">The teams to save</param>
    private async Task SaveTeamData(IEnumerable<Team> teams)
    {
        await _teamRepo.AddUpdateTeams(teams);
        await _teamRepo.AddUpdateSeasonTeams(teams);
        await _teamRepo.Commit();
    }

    /// <summary>
    /// Saves the season schedule data
    /// </summary>
    /// <param name="seasonStartYear">The season start year</param>
    /// <param name="seasonGameCount">The game count for the season</param>
    private async Task SaveSeasonSchedule(int seasonStartYear, int seasonGameCount)
    {
        await _gameRepo.AddUpdateSeasonGameCount(seasonStartYear, seasonGameCount);
        await _gameRepo.Commit();
    }

    /// <summary>
    /// Whether we can skip the season or not
    /// </summary>
    /// <param name="mode">Whether we are adding games or can also update</param>
    /// <param name="isCurrentYear">Whether the season is the current year</param>
    /// <param name="hasAllSeasonGames">Whether all games have been found for a season</param>
    /// <returns>True if the season can be skipped. Otherwise false</returns>
    private static bool CanSkipSeason(ModeType mode, bool isCurrentYear, bool hasAllSeasonGames)
    {
        return hasAllSeasonGames && !isCurrentYear && mode != ModeType.NhlUpdate;
    }

    /// <summary>
    /// Saves a list of players to the database
    /// </summary>
    /// <param name="players">Players to save</param>
    private async Task SavePlayers(IEnumerable<Player> players)
    {
        _logger.LogInformation("Saving Players");
        await _playerRepo.AddUpdatePlayers(players);
        await _playerRepo.AddUpdatePlayerDraftDetails(players);
        // Save all data to the database
        await _gameRepo.Commit();
    }

    /// <summary>
    /// Saves the game to the database
    /// </summary>
    /// <param name="game">The game to save</param>
    private async Task SaveGame(Game game)
    {
        _logger.LogInformation("Saving Game: " + game.Id);
        await _gameRepo.AddUpdateGame(game);

        // Broadcasters and game events only exist for played games
        if (game.HasBeenPlayed)
        {
            await _broadcasterRepo.AddUpdateTvBroadcasters(game);
            await _broadcasterRepo.AddUpdateGameTvBroadcasters(game);
            await _gameEventRepo.AddUpdateGameEvents(game);
        }

        // Roster stats, coaches, and officials are only available for played games
        if (game.HasBeenPlayed)
        {
            await _playerRepo.AddUpdateGameRosterStats(game);
            await _gameRepo.AddUpdateGameCoaches(game);
            await _gameRepo.AddUpdateGameOfficials(game);
        }

        // Save all data to the database
        await _gameRepo.Commit();
    }

    /// <summary>
    /// Gets if all of a seasons games are already found
    /// </summary>
    /// <param name="seasonStartYear">Season to check</param>
    /// <returns>True if all games exist, otherwise False</returns>
    private async Task<bool> HasAllSeasonGames(int seasonStartYear)
    {
        var gameCount = await _gameRepo.GetSavedGameCountForSeason(seasonStartYear);
        var seasonGameCount = await _gameRepo.GetGameCountForSeason(seasonStartYear);
        if (!(seasonGameCount > 0 && gameCount >= seasonGameCount)) return false;
        if (!await _gameRepo.HasPlayoffGamesForSeason(seasonStartYear)) return false;
        // Stale or placeholder playoff games keep the season open after the Sept 15 rollover,
        // otherwise they'd never be re-fetched or cleaned up
        return !await _gameRepo.HasPastUnplayedGamesForSeason(seasonStartYear);
    }
}