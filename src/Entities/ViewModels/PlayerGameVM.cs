namespace Entities.ViewModels;

/// <summary>One of a player's games: who his team played, how it ended, and his line.</summary>
public class PlayerGameVM
{
    public int GameId { get; set; }
    public DateTime GameDate { get; set; }
    public int GameType { get; set; }
    public int TeamId { get; set; }
    public bool IsHome { get; set; }
    public PlayerTeamVM Opponent { get; set; } = new();
    public int TeamGoals { get; set; }
    public int OpponentGoals { get; set; }
    // "W", "L" or "OTL" (lost in overtime or a shootout)
    public string Result { get; set; } = string.Empty;
    public PlayerGameStatsVM Stats { get; set; } = new();
}
