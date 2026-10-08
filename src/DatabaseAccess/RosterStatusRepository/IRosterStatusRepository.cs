using Entities.DbModels;

namespace DatabaseAccess.RosterStatusRepository;

public record RosterReportCandidate(int PlayerId, string FirstName, string LastName, DateTime BirthDate, int LatestTeamId);

public interface IRosterStatusRepository
{
    /// <summary>Players born on one of the given dates who have stats since the given season, with the team of their latest game.</summary>
    Task<List<RosterReportCandidate>> GetCandidates(IEnumerable<DateTime> birthDates, int sinceSeasonStartYear);
    Task AddSnapshot(IEnumerable<DbRosterStatus> statuses);
}