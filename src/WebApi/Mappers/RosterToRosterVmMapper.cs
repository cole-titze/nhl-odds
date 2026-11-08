using Entities.Models.Web;
using Entities.Types;
using Entities.ViewModels;

namespace WebApi.Mappers;

/// <summary>Rosters and game players, listed forwards, then defense, then goalies, each by sweater number.</summary>
public static class RosterToRosterVmMapper
{
    public static List<RosterPlayerVM> Map(IEnumerable<RosterPlayer> roster) =>
        roster
            .OrderBy(p => PositionGroup(p.Position))
            .ThenBy(p => p.SweaterNumber ?? short.MaxValue)
            .ThenBy(p => p.LastName)
            .Select(p => new RosterPlayerVM
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                HeadShot = p.HeadShot,
                Position = PlayerToPlayerVmMapper.PositionCode(p.Position),
                SweaterNumber = p.SweaterNumber,
                GamesPlayed = p.GamesPlayed,
                IsInjuredReserve = p.IsInjuredReserve,
            })
            .ToList();

    public static GamePlayersVM MapGame(GamePlayers players)
    {
        var game = players.Game;
        var hasStats = players.Lines.Count > 0;
        var all = hasStats
            ? players.Lines.Select(l => (l.TeamId, l.Position, l.SweaterNumber, l.LastName, Vm: new GamePlayerVM
            {
                Id = l.PlayerId,
                FirstName = l.FirstName,
                LastName = l.LastName,
                Position = PlayerToPlayerVmMapper.PositionCode(l.Position),
                SweaterNumber = l.SweaterNumber,
                Stats = PlayerToPlayerVmMapper.MapStats(l),
            }))
            : players.Roster.Select(p => (p.TeamId, p.Position, p.SweaterNumber, p.LastName, Vm: new GamePlayerVM
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Position = PlayerToPlayerVmMapper.PositionCode(p.Position),
                SweaterNumber = p.SweaterNumber,
            }));
        var ordered = all
            .OrderBy(x => PositionGroup(x.Position))
            .ThenBy(x => x.SweaterNumber ?? short.MaxValue)
            .ThenBy(x => x.LastName)
            .ToList();
        return new GamePlayersVM
        {
            GameId = game.Id,
            HasStats = hasStats,
            Home = ordered.Where(x => x.TeamId == game.HomeTeam.Id).Select(x => x.Vm).ToList(),
            Away = ordered.Where(x => x.TeamId == game.AwayTeam.Id).Select(x => x.Vm).ToList(),
        };
    }

    private static int PositionGroup(POSITION position) => position switch
    {
        POSITION.Defenseman => 1,
        POSITION.Goalie => 2,
        _ => 0,
    };
}
