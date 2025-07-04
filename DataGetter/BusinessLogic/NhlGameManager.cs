using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using Entities.Models;
using Entities.Types;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;

using Services.NhlData;

namespace DataGetter.BusinessLogic;

public class NhlGameManager
{
    private readonly IGameRepository _gameRepo;
    private readonly IPlayerRepository _playerRepo;
    private readonly NhlDataGetter _nhlDataGetter;
    private readonly ILogger<NhlGameManager> _logger;
    public NhlGameManager(IGameRepository gameRepository, IPlayerRepository playerRepository, NhlDataGetter nhlDataGetter, ILoggerFactory loggerFactory)
    {
        _gameRepo = gameRepository;
        _playerRepo = playerRepository;
        _nhlDataGetter = nhlDataGetter;
        _logger = loggerFactory.CreateLogger<NhlGameManager>();
    }

    /// <summary>
    /// Gets all nhl games within the season range.
    /// </summary>
    /// <param name="seasonYearRange">The years to get data for</param>
    public async Task GetGameData(YearRange seasonYearRange, ModeType mode)
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

            await FetchAndSaveNhlGameData(seasonStartYear, mode);
        }
    }

    /// <summary>
    /// Gets all game and player data for a given season and stores it to the database
    /// </summary>
    /// <param name="seasonStartYear">Season to get data for</param>
    /// <param name="mode">Whether we are adding games, or also updating</param>
    private async Task FetchAndSaveNhlGameData(int seasonStartYear, ModeType mode)
    {
        var seasonGameCount = await _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(seasonStartYear);
        await _gameRepo.AddUpdateSeasonGameCount(seasonStartYear, seasonGameCount);
        await _gameRepo.Commit();

        Game? game;
        // game ids start at 1
        for (int count = 1; count <= seasonGameCount; count++)
        {
            var gameId = NhlDataGetter.GetGameId(seasonStartYear, count);
            var existingGame = await _gameRepo.GetGame(gameId);
            if (CanSkipGame(existingGame, mode))
                continue;

            game = await _nhlDataGetter.GameDataGetter.GetGame(gameId);
            if (game != null)
            {
                // TODO: Give this one more look
                await BuildGameRosterStats(game);
                var gamePlayers = await GetPlayers(game);

                await SavePlayers(gamePlayers);
                await SaveGame(game);
            }
        }
    }
    /// <summary>
    /// Checks if a game can be skipped based on the existing game data and the mode type.
    /// </summary>
    /// <param name="existingGame">The game to check</param>
    /// <param name="mode">Whether we are updating or adding games</param>
    /// <returns>True if the game can be skipped, otherwise false</returns>
    private static bool CanSkipGame(Game? existingGame, ModeType mode)
    {
        return existingGame != null && existingGame.HasBeenPlayed && mode != ModeType.Update;
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
        var seasonGameCount = await _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(seasonStartYear);
        return gameCount == seasonGameCount;
    }

    /// Gets a seasons worth of player stats per game. Only returns games that have not already been found.
    /// </summary>
    /// <param name="seasonGames">Games to get player stats for</param>
    /// <returns>List of player game stats from the start year</returns>
    private async Task<GameRosterStats> BuildGameRosterStats(Game game)
    {
        var seasonPlayerGameStats = new List<GameRosterStats>();
        GameRosterStats? gameRosterStats;

        gameRosterStats = await _nhlDataGetter.PlayerDataGetter.BuildGameRosterStats(game);
        game.RosterStats = gameRosterStats;
        if (gameRosterStats == null)
        {
            throw new Exception("Failed to get game roster stats for game: " + game.Id);
        }

        return gameRosterStats;
    }

    /// Gets a seasons worth of players. Only returns players that are active
    /// </summary>
    /// <param name="game">The game to get players for</param>
    /// <returns>List of players for the game</returns>
    // TODO: Combine the for loops (should only need two)
    private async Task<IEnumerable<Player>> GetPlayers(Game game)
    {
        _logger.LogInformation("Getting Players for game: " + game.Id.ToString());

        var uniqueHomePlayerIds = new HashSet<int>();
        var uniqueAwayPlayerIds = new HashSet<int>();
        var gameRosterStats = game.RosterStats ?? new GameRosterStats();
        foreach (var playerStats in gameRosterStats.AllHomeTeamPlayers)
        {
            uniqueHomePlayerIds.Add(playerStats.PlayerId);
        }
        foreach (var playerStats in gameRosterStats.AllAwayTeamPlayers)
        {
            uniqueAwayPlayerIds.Add(playerStats.PlayerId);
        }

        var players = new List<Player>();
        foreach (var playerId in uniqueHomePlayerIds)
        {
            var player = await _nhlDataGetter.PlayerDataGetter.GetPlayer(playerId, game.HomeTeamId);
            if (player != null)
                players.Add(player);
        }
        foreach (var playerId in uniqueAwayPlayerIds)
        {
            var player = await _nhlDataGetter.PlayerDataGetter.GetPlayer(playerId, game.AwayTeamId);
            if (player != null)
                players.Add(player);
        }

        return players;
    }
}
