using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using Entities.Models;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;

using Services.NhlData;

namespace DataGetter.BusinessLogic;

public class NhlGameManager
{
    private readonly IGameRepository _gameRepo;
    private readonly IPlayerRepository _playerRepo;
    private readonly NhlApiDataGetter _nhlDataGetter;
    private readonly ILogger<NhlGameManager> _logger;
    public NhlGameManager(IGameRepository gameRepository, IPlayerRepository playerRepository, NhlApiDataGetter nhlDataGetter, ILoggerFactory loggerFactory)
    {
        _gameRepo = gameRepository;
        _playerRepo = playerRepository;
        _nhlDataGetter = nhlDataGetter;
        _logger = loggerFactory.CreateLogger<NhlGameManager>();
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

    /// Gets a seasons worth of player stats per game. Only returns games that have not already been found.
    /// </summary>
    /// <param name="seasonGames">Games to get player stats for</param>
    /// <returns>List of player game stats from the start year</returns>
    private async Task<GameRosterStats> BuildGameRosterStats(Game game)
    {
        GameRosterStats? gameRosterStats;

        gameRosterStats = await _nhlDataGetter.PlayerDataGetter.BuildGameRosterStats(game);
        game.RosterStats = gameRosterStats;
        if (gameRosterStats == null)
        {
            throw new Exception("Failed to get game roster stats for game: " + game.Id);
        }

        return gameRosterStats;
    }

    /// <summary>
    /// Gets the season game count
    /// </summary>
    /// <param name="seasonStartYear">The season to get the game count for</param>
    /// <param name="mode">Whether we can update games or only add</param>
    /// <returns>The game count for the season</returns>
    public async Task<int> GetSeasonGameCount(int seasonStartYear, ModeType mode)
    {
        var existingGameCount = await _gameRepo.GetGameCountForSeason(seasonStartYear);
        if (existingGameCount != null && mode != ModeType.Update)
            return (int)existingGameCount;

        var gameCount = await _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(seasonStartYear);
        if (gameCount == null)
        {
            throw new Exception("Failed to get season game count for season: " + seasonStartYear);
        }

        return (int)gameCount;
    }

    /// <summary>
    /// Gets the game for the given Id
    /// </summary>The game id of the game to get</param>
    /// <param name="mode">Whether we are updating the game, or only adding</param>
    /// <returns>The game</returns>
    public async Task<Game?> GetGame(int gameId, ModeType mode)
    {
        var existingGame = await _gameRepo.GetGame(gameId);
        if (CanSkipGame(existingGame, mode))
        {
            _logger.LogInformation("Game {GameId} already exists and has been played. Skipping fetch.", gameId);
            return existingGame!;
        }

        var game = await _nhlDataGetter.GameDataGetter.GetGame(gameId);
        if (game == null)
        {
            _logger.LogInformation("Game {GameId} is not available yet. Skipping.", gameId);
            return null;
        }

        if (!game.HasBeenPlayed)
        {
            return game;
        }

        await BuildGameRosterStats(game);

        return game;
    }
}
