using Entities.DbModels;

namespace DatabaseAccess.GameDetailRepository;

public interface IGameDetailRepository
{
    /// <summary>Played games in the season without a fetch row of this kind ("Roster" or "Shifts"); with
    /// <paramref name="includeEmpty"/>, also those whose last fetch had no rows.</summary>
    Task<List<int>> GetPlayedGamesWithoutFetch(int seasonStartYear, string kind, bool includeEmpty = false);
    /// <summary>Every played game in the season.</summary>
    Task<List<int>> GetPlayedGames(int seasonStartYear);
    /// <summary>Every goal in the season with a replay URL: game id, event id, URL.</summary>
    Task<List<(int GameId, int EventId, string Url)>> GetGoalsWithReplayUrl(int seasonStartYear);
    /// <summary>Every player id.</summary>
    Task<List<int>> GetPlayerIds();
    /// <summary>Goals in the season with a replay URL and no GoalReplay row: game id, event id, URL.</summary>
    Task<List<(int GameId, int EventId, string Url)>> GetGoalsWithoutReplay(int seasonStartYear);
    /// <summary>Replaces the game's roster rows and records the fetch.</summary>
    Task ReplaceRoster(int gameId, IReadOnlyCollection<DbGameRosterSpot> spots, DateTime fetchedUtc);
    /// <summary>Replaces the game's shift rows and records the fetch.</summary>
    Task ReplaceShifts(int gameId, IReadOnlyCollection<DbGameShift> shifts, DateTime fetchedUtc);
    /// <summary>Replaces the goal's replay and its positions.</summary>
    Task ReplaceReplay(DbGoalReplay replay, IReadOnlyCollection<DbGoalReplayPosition> positions);
}