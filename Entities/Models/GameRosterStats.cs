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
            return AllHomeTeamPlayers
                .Concat(AllAwayTeamPlayers);
        }
    }
    /// <summary>
    /// Enumerates all players from the home team (forwards, defensemen, goalies).
    /// </summary>
    /// <returns>An enumerable collection of all home players in the game.</returns>
    public IEnumerable<IGamePlayerStats> AllHomeTeamPlayers
    {
        get
        {
            return HomeTeamForwards
                .Concat(HomeTeamDefensemen)
                .Concat(HomeTeamGoalies);
        }
    }
    /// <summary>
    /// Enumerates all players from the away team (forwards, defensemen, goalies).
    /// </summary>
    /// <returns>An enumerable collection of all away players in the game.</returns>
    public IEnumerable<IGamePlayerStats> AllAwayTeamPlayers
    {
        get
        {
            return AwayTeamForwards
                .Concat(AwayTeamDefensemen)
                .Concat(AwayTeamGoalies);
        }
    }
}