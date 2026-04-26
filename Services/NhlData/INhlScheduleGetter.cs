using Entities.Models.Teams;

namespace Services.NhlData;

public interface INhlScheduleGetter
{
    Task<int?> GetGameCountInSeason(int year);
    IDictionary<int, int> GetSeasonGameCounts();
    Task<IEnumerable<SeasonTeam>?> GetTeamsForSeason(int seasonStartYear);
    Task<IEnumerable<Team>?> GetAllTeams();
}