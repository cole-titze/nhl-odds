using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DatabaseAccess.RosterStatusRepository;
using Entities.DbModels;
using Entities.ServiceModels;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Saves a snapshot of the NHL roster report (who is Active and who is on Injured Reserve) so the cleaner can
/// drop injured players from projected lineups. The report has no ids: players are matched by birthdate and
/// name, and each team section is matched to the team most of its players last played for.
/// </summary>
public partial class NhlRosterStatusManager
{
    // A team section is only used when this many of its players matched
    private const int MIN_MATCHED_PER_TEAM = 5;
    private readonly IRosterStatusRepository _rosterStatusRepo;
    private readonly INhlRosterReportGetter _reportGetter;
    private readonly ILogger<NhlRosterStatusManager> _logger;

    public NhlRosterStatusManager(IRosterStatusRepository rosterStatusRepository, INhlRosterReportGetter reportGetter, ILoggerFactory loggerFactory)
    {
        _rosterStatusRepo = rosterStatusRepository;
        _reportGetter = reportGetter;
        _logger = loggerFactory.CreateLogger<NhlRosterStatusManager>();
    }

    public async Task SaveRosterSnapshot(DateTime nowUtc)
    {
        var entries = await _reportGetter.GetRosterReport();
        if (entries.Count == 0)
        {
            _logger.LogWarning("Roster report was empty; no snapshot saved");
            return;
        }

        var seasonStartYear = nowUtc.Month >= 7 ? nowUtc.Year : nowUtc.Year - 1;
        var candidates = await _rosterStatusRepo.GetCandidates(entries.Select(e => e.BirthDate), seasonStartYear - 1);
        var snapshotUtc = nowUtc.AddTicks(-(nowUtc.Ticks % TimeSpan.TicksPerSecond));
        var (statuses, unmatched) = MatchEntries(entries, candidates, snapshotUtc);

        await _rosterStatusRepo.AddSnapshot(statuses);
        _logger.LogInformation(
            "Saved roster snapshot: {Matched} players ({InjuredReserve} on IR), {Unmatched} report rows unmatched",
            statuses.Count, statuses.Count(s => s.IsInjuredReserve), unmatched);
    }

    /// <summary>
    /// Matches report rows to players with stats and assigns each team section a team id. Rows that match no
    /// player (or more than one) are skipped, as are sections with too few matches to identify the team.
    /// </summary>
    public static (List<DbRosterStatus> Statuses, int Unmatched) MatchEntries(
        IEnumerable<ServiceRosterReportEntry> entries, IEnumerable<RosterReportCandidate> candidates, DateTime snapshotUtc)
    {
        var byBirthDate = candidates.ToLookup(c => c.BirthDate.Date);
        var statuses = new List<DbRosterStatus>();
        var seen = new HashSet<int>();
        int unmatched = 0;

        foreach (var section in entries.GroupBy(e => e.TeamName))
        {
            var matched = new List<(ServiceRosterReportEntry Entry, RosterReportCandidate Player)>();
            foreach (var entry in section)
            {
                var player = MatchPlayer(entry, byBirthDate[entry.BirthDate.Date]);
                if (player == null)
                    unmatched++;
                else
                    matched.Add((entry, player));
            }

            var teamVotes = matched.GroupBy(m => m.Player.LatestTeamId).MaxBy(g => g.Count());
            if (teamVotes == null || teamVotes.Count() < MIN_MATCHED_PER_TEAM)
            {
                unmatched += matched.Count;
                continue;
            }

            foreach (var (entry, player) in matched)
            {
                if (!seen.Add(player.PlayerId))
                    continue;

                statuses.Add(new DbRosterStatus
                {
                    SnapshotUTC = snapshotUtc,
                    PlayerId = player.PlayerId,
                    TeamId = teamVotes.Key,
                    IsInjuredReserve = entry.IsInjuredReserve,
                    InjuredReserveDate = entry.InjuredReserveDate,
                    ReportUpdated = entry.ReportUpdated,
                });
            }
        }

        return (statuses, unmatched);
    }

    private static RosterReportCandidate? MatchPlayer(ServiceRosterReportEntry entry, IEnumerable<RosterReportCandidate> sameBirthDate)
    {
        var reportWords = NormalizeName(NicknamePattern().Replace(entry.Name, " ")).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var reportName = " " + string.Join(' ', reportWords) + " ";
        var lastNameMatches = sameBirthDate
            .Where(c => reportName.EndsWith(" " + NormalizeName(c.LastName) + " "))
            .ToList();
        if (lastNameMatches.Count <= 1)
            return lastNameMatches.SingleOrDefault();

        // Same birthdate and last name: fall back to the first name
        var fullMatches = lastNameMatches
            .Where(c => reportName.StartsWith(" " + NormalizeName(c.FirstName) + " "))
            .ToList();
        return fullMatches.Count == 1 ? fullMatches[0] : null;
    }

    /// <summary>Uppercase letters only, accents removed, words separated by single spaces.</summary>
    private static string NormalizeName(string name)
    {
        var decomposed = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetter(ch) ? char.ToUpperInvariant(ch) : ' ');
        }
        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // "ANTHONY-JOHN (AJ) GREER" -> "ANTHONY-JOHN GREER"
    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex NicknamePattern();
}