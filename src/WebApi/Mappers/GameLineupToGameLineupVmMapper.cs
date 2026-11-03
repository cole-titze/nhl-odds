using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class GameLineupToGameLineupVmMapper
{
    public static GameLineupVM Map(GameLineup lineup) => new()
    {
        GameId = lineup.GameId,
        UpdatedUTC = lineup.UpdatedUTC,
        VersionCount = lineup.VersionCount,
        StatusReport = lineup.StatusReport,
        Away = MapTeam(lineup.Players.Where(p => p.Side == "Away")),
        Home = MapTeam(lineup.Players.Where(p => p.Side == "Home")),
    };

    private static TeamLineupVM MapTeam(IEnumerable<GameLineupPlayer> players)
    {
        var byGroup = players.ToLookup(p => p.Group);
        return new TeamLineupVM
        {
            Forwards = Lines(byGroup["F"]),
            Defense = Lines(byGroup["D"]),
            Goalies = List(byGroup["G"]),
            Scratched = List(byGroup["Scratched"]),
            Injured = List(byGroup["Injured"]),
            Suspended = List(byGroup["Suspended"]),
        };
    }

    private static List<List<LineupPlayerVM>> Lines(IEnumerable<GameLineupPlayer> players) => players
        .GroupBy(p => p.LineNumber)
        .OrderBy(line => line.Key)
        .Select(line => line.OrderBy(p => p.Slot).Select(Player).ToList())
        .ToList();

    private static List<LineupPlayerVM> List(IEnumerable<GameLineupPlayer> players) =>
        players.OrderBy(p => p.LineNumber).ThenBy(p => p.Slot).Select(Player).ToList();

    private static LineupPlayerVM Player(GameLineupPlayer p) => new() { Name = p.Name, PlayerId = p.PlayerId, Note = p.Note };
}
