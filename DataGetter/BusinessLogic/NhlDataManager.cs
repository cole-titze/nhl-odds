using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using DatabaseAccess.TeamRepository;
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
    private readonly NhlTeamManager _teamManager;
    private readonly NhlGameManager _gameManager;
    private readonly NhlPlayerManager _playerManager;
    private readonly ILogger<NhlDataManager> _logger;
    public NhlDataManager(IGameRepository gameRepository, IPlayerRepository playerRepository, ITeamRepository teamRepository, NhlGameManager gameManager, NhlPlayerManager playerManager, NhlTeamManager teamManager, ILoggerFactory loggerFactory)
    {
        _gameRepo = gameRepository;
        _playerRepo = playerRepository;
        _teamRepo = teamRepository;
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
    // TODO: Respect mode for updating season game count
    private async Task FetchAndSaveSeasonData(int seasonStartYear, ModeType mode)
    {
        var seasonTeams = await _teamManager.GetTeamData(seasonStartYear, mode);
        await SaveTeamData(seasonTeams);

        var seasonGameCount = await _gameManager.GetSeasonGameCount(seasonStartYear);
        await SaveSeasonSchedule(seasonStartYear, seasonGameCount);

        // game ids start at 1
        seasonGameCount = 10; // TODO: Remove this line
        for (int count = 1; count <= seasonGameCount; count++)
        {
            var gameId = NhlApiDataGetter.GetGameId(seasonStartYear, count);
            var game = await _gameManager.GetGame(gameId, mode);
            var players = await _playerManager.GetPlayers(game, mode);

            await SavePlayers(players);
            await SaveGame(game);
            await _gameRepo.Commit();
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
        return hasAllSeasonGames && !isCurrentYear && mode != ModeType.Update;
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

        // Updates tv broadcasters for the games
        await _gameRepo.AddUpdateTvBroadcasters(game);
        await _gameRepo.AddUpdateGameTvBroadcasters(game);

        // Update game events and save to the db
        await _gameRepo.AddUpdateGameEvents(game);
        await _playerRepo.AddUpdateGameRosterStats(game);

        // Add Officials
        await _gameRepo.AddUpdateGameOfficials(game);

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
        return gameCount == seasonGameCount;
    }
}
