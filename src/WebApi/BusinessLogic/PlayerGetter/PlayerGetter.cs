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

    public async Task<List<ActivePlayerVM>> GetSitemapPlayers(DateTime nowUtc)
    {
        var players = await _playerRepository.GetPlayersLastGames();
        return players
            .Where(p => p.LastGameDateUTC <= nowUtc)
            .OrderBy(p => p.Id)
            .Select(p => new ActivePlayerVM { Id = p.Id, LastGameDate = p.LastGameDateUTC })
            .ToList();
    }

    public async Task<List<PlayerSearchResultVM>> FindPlayers(string name, int seasonStartYear, int limit)
    {
        var names = await _playerRepository.GetPlayerNames();
        // Before the season's first game, last season's players count as active
        var active = await _playerRepository.GetActivePlayers(seasonStartYear);
        if (active.Count == 0)
            active = await _playerRepository.GetActivePlayers(seasonStartYear - 1);
        var activeIds = active.Select(p => p.Id).ToHashSet();

        var matches = PlayerNameMatcher.Match(names, name, activeIds, limit);
        var teams = (await _playerRepository.GetLatestTeams(matches.Select(p => p.Id).ToList()))
            .ToDictionary(t => t.PlayerId);
        return matches.Select(p =>
        {
            teams.TryGetValue(p.Id, out var team);
            return new PlayerSearchResultVM
            {
                Id = p.Id,
                Name = $"{p.FirstName} {p.LastName}",
                Position = team != null ? PlayerToPlayerVmMapper.PositionCode(team.Position) : null,
                CurrentTeamId = team?.CurrentTeamId,
                CurrentTeamAbbreviation = team?.CurrentTeamAbbreviation,
                LastTeamAbbreviation = team?.TeamAbbreviation,
                IsActive = activeIds.Contains(p.Id),
            };
        }).ToList();
    }
}
