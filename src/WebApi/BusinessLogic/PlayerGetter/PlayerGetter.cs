using DatabaseAccess.WebPlayerRepository;
using Entities.ViewModels;
using WebApi.Mappers;

namespace WebApi.BusinessLogic.PlayerGetter;

public class PlayerGetter : IPlayerGetter
{
    private readonly IWebPlayerRepository _playerRepository;

    public PlayerGetter(IWebPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<PlayerVM?> GetPlayer(int playerId, int seasonStartYear)
    {
        var player = await _playerRepository.GetPlayer(playerId);
        if (player == null)
            return null;
        var games = await _playerRepository.GetPlayerGames(playerId, seasonStartYear);
        return PlayerToPlayerVmMapper.Map(player, games, seasonStartYear);
    }

    public async Task<List<RosterPlayerVM>> GetTeamRoster(int teamId, int seasonStartYear)
    {
        var roster = await _playerRepository.GetTeamRoster(teamId, seasonStartYear);
        return RosterToRosterVmMapper.Map(roster);
    }

    public async Task<GamePlayersVM?> GetGamePlayers(int gameId)
    {
        var players = await _playerRepository.GetGamePlayers(gameId);
        return players == null ? null : RosterToRosterVmMapper.MapGame(players);
    }

    public async Task<List<ActivePlayerVM>> GetActivePlayers(int seasonStartYear)
    {
        var players = await _playerRepository.GetActivePlayers(seasonStartYear);
        return players.Select(p => new ActivePlayerVM { Id = p.Id, LastGameDate = p.LastGameDateUTC }).ToList();
    }
}
