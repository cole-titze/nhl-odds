using DatabaseAccess.WebPlayerRepository;
using Entities.Models.Web;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeWebPlayerRepository : IWebPlayerRepository
{
    public List<PlayerProfile> Players { get; } = new();
    public List<PlayerGameLine> Lines { get; } = new();
    public List<RosterPlayer> Roster { get; } = new();
    public List<GamePlayers> Games { get; } = new();
    public List<ActivePlayer> Active { get; } = new();

    public Task<PlayerProfile?> GetPlayer(int playerId) =>
        Task.FromResult(Players.FirstOrDefault(p => p.Id == playerId));

    public Task<List<PlayerGameLine>> GetPlayerGames(int playerId, int seasonStartYear) =>
        Task.FromResult(Lines.Where(l => l.PlayerId == playerId && l.Game.SeasonStartYear == seasonStartYear).ToList());

    public Task<List<RosterPlayer>> GetTeamRoster(int teamId, int seasonStartYear) =>
        Task.FromResult(Roster.Where(p => p.TeamId == teamId).ToList());

    public Task<GamePlayers?> GetGamePlayers(int gameId) =>
        Task.FromResult(Games.FirstOrDefault(g => g.Game.Id == gameId));

    public Task<List<ActivePlayer>> GetActivePlayers(int seasonStartYear) => Task.FromResult(Active.ToList());
}
