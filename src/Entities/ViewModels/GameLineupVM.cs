namespace Entities.ViewModels;

/// <summary>The Game page's lineup endpoint: Lineup is null when NHL.com hasn't projected one</summary>
public class GameLineupResultVM
{
    public GameLineupVM? Lineup { get; set; }
}

public class GameLineupVM
{
    public int GameId { get; set; }
    public DateTime UpdatedUTC { get; set; }
    public int VersionCount { get; set; }
    public string? StatusReport { get; set; }
    public TeamLineupVM Away { get; set; } = new();
    public TeamLineupVM Home { get; set; } = new();
}

public class TeamLineupVM
{
    /// <summary>Forward lines, each left wing, center, right wing as listed</summary>
    public List<List<LineupPlayerVM>> Forwards { get; set; } = [];
    public List<List<LineupPlayerVM>> Defense { get; set; } = [];
    /// <summary>In NHL.com's order</summary>
    public List<LineupPlayerVM> Goalies { get; set; } = [];
    public List<LineupPlayerVM> Scratched { get; set; } = [];
    public List<LineupPlayerVM> Injured { get; set; } = [];
    public List<LineupPlayerVM> Suspended { get; set; } = [];
}

public class LineupPlayerVM
{
    public string Name { get; set; } = string.Empty;
    public int? PlayerId { get; set; }
    /// <summary>The injury for injured players, e.g. "upper body"</summary>
    public string? Note { get; set; }
}
