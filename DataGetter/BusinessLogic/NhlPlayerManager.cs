using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using Entities.Models;
using Entities.Types;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;

using Services.NhlData;

namespace DataGetter.BusinessLogic;

public class NhlPlayerManager
{
    private readonly IPlayerRepository _playerRepo;
    private readonly NhlApiDataGetter _nhlDataGetter;
    private readonly ILogger<NhlPlayerManager> _logger;
    public NhlPlayerManager(IPlayerRepository playerRepository, NhlApiDataGetter nhlDataGetter, ILoggerFactory loggerFactory)
    {
        _playerRepo = playerRepository;
        _nhlDataGetter = nhlDataGetter;
        _logger = loggerFactory.CreateLogger<NhlPlayerManager>();
    }

    /// Gets a seasons worth of players. Only returns players that are active
    /// </summary>
    /// <param name="game">The game to get players for</param>
    /// <param name="mode">Whether to only add new players, or also update
    /// /// <returns>List of players for the game</returns>
    public async Task<IEnumerable<Player>> GetPlayers(Game game, ModeType mode)
    {
        _logger.LogInformation("Getting Players for game: " + game.Id.ToString());

        var players = new List<Player>();
        var gameRosterStats = game.RosterStats ?? new GameRosterStats();
        players.AddRange(await GetPlayersForTeam(gameRosterStats.AllHomeTeamPlayers, game.HomeTeamId, mode));
        players.AddRange(await GetPlayersForTeam(gameRosterStats.AllAwayTeamPlayers, game.AwayTeamId, mode));

        return players;
    }

    /// <summary>
    /// Gets the player data for the given game
    /// </summary>
    /// <param name="teamPlayers">The teams players to get</param>
    /// <param name="teamId">The team id</param>
    /// <param name="mode">Whether to only add new players or also update existing players</param>
    /// <returns>The list of players that were fetched</returns>
    private async Task<IEnumerable<Player>> GetPlayersForTeam(IEnumerable<IGamePlayerStats> teamPlayers, int teamId, ModeType mode)
    {
        var players = new List<Player>();
        foreach (var playerStats in teamPlayers)
        {
            var dbPlayer = await _playerRepo.GetPlayer(playerStats.PlayerId);
            var hasPlayer = dbPlayer != null;
            if (CanSkipPlayer(mode, hasPlayer))
            {
                _logger.LogInformation("Player: " + playerStats.PlayerId + " already exists Skipping...");
                continue;
            }
            var player = await _nhlDataGetter.PlayerDataGetter.GetPlayer(playerStats.PlayerId, teamId);
            if (player != null)
                players.Add(player);
        }

        return players;
    }

    /// <summary>
    /// Determines whether the player can be skipped or not
    /// </summary>
    /// <param name="mode">Whether we are updating existing data or only adding new</param>
    /// <param name="hasPlayer">Whether the player already exists or not</param>
    /// <returns>True if the player can be skipped, otherwise false</returns>
    private static bool CanSkipPlayer(ModeType mode, bool hasPlayer)
    {
        return mode != ModeType.Update && hasPlayer;
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
        await _playerRepo.Commit();
    }
}
