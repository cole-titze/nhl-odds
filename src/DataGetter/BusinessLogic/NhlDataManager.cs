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

        // Playoff games have no clean schedule total — iterate until 404s in a row stop us.
        await FetchSegment(seasonStartYear, mode,
            n => NhlApiDataGetter.GetPlayoffGameId(seasonStartYear, n), knownCount: null);
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
                    Message = ex.Message,
                    StackTrace = topFrames,
                    Source = "FetchSegment"
                };
                await _errorRepo.AddError(errorLog);
            }

            count++;
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

        await _playerRepo.AddUpdateGameRosterStats(game);

        // Coaches and officials are only available for played games
        if (game.HasBeenPlayed)
        {
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
        return seasonGameCount > 0 && gameCount == seasonGameCount;
    }
}