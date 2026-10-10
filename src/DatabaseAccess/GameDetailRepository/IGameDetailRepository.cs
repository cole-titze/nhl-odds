using Entities.DbModels;

namespace DatabaseAccess.GameDetailRepository;

public interface IGameDetailRepository
{
    /// <summary>Played games in the season without a fetch row of this kind ("Roster" or "Shifts"); with
    /// <paramref name="includeEmpty"/>, also those whose last fetch had no rows.</summary>
    Task<List<int>> GetPlayedGamesWithoutFetch(int seasonStartYear, string kind, bool includeEmpty = false);
    /// <summary>Goals in the season with a replay URL and no GoalReplay row: game id, event id, URL.</summary>
    Task<List<(int GameId, int EventId, string Url)>> GetGoalsWithoutReplay(int seasonStartYear);
    /// <summary>Replaces the game's roster rows and records the fetch.</summary>
    Task ReplaceRoster(int gameId, IReadOnlyCollection<DbGameRosterSpot> spots, DateTime fetchedUtc);
    /// <summary>Replaces the game's shift rows and records the fetch.</summary>
    Task ReplaceShifts(int gameId, IReadOnlyCollection<DbGameShift> shifts, DateTime fetchedUtc);
    /// <summary>Replaces the goal's replay and its positions.</summary>
    Task ReplaceReplay(DbGoalReplay replay, IReadOnlyCollection<DbGoalReplayPosition> positions);
}