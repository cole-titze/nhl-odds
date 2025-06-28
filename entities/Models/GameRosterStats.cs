namespace Entities.Models;

/// <summary>
/// Represents the stats for a game roster, including teams, coaches, and players.
/// </summary>
public class GameRosterStats
{
    public Coach HomeTeamCoach { get; set; } = new Coach();
    public IEnumerable<IGamePlayerStats> HomeTeamForwards { get; set; } = new List<IGamePlayerStats>();
    public IEnumerable<IGamePlayerStats> HomeTeamDefensemen { get; set; } = new List<IGamePlayerStats>();
    public IEnumerable<IGamePlayerStats> HomeTeamGoalies { get; set; } = new List<IGamePlayerStats>();
    public Coach AwayTeamCoach { get; set; } = new Coach();
    public IEnumerable<IGamePlayerStats> AwayTeamForwards { get; set; } = new List<IGamePlayerStats>();
    public IEnumerable<IGamePlayerStats> AwayTeamDefensemen { get; set; } = new List<IGamePlayerStats>();
    public IEnumerable<IGamePlayerStats> AwayTeamGoalies { get; set; } = new List<IGamePlayerStats>();
    public IEnumerable<IOfficial> Referees { get; set; } = new List<IOfficial>();
    public IEnumerable<IOfficial> Linesmen { get; set; } = new List<IOfficial>();

    /// <summary>
    /// Enumerates all players (forwards, defensemen, goalies) from both teams.
    /// </summary>
    /// <returns>An enumerable collection of all players in the game.</returns>
    public IEnumerable<IGamePlayerStats> AllPlayers
    {
        get
        {
            return HomeTeamForwards
                .Concat(HomeTeamDefensemen)
                .Concat(HomeTeamGoalies)
                .Concat(AwayTeamForwards)
                .Concat(AwayTeamDefensemen)
                .Concat(AwayTeamGoalies);
        }
    }
}

