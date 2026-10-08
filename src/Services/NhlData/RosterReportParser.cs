using System.Globalization;
using System.Text.RegularExpressions;
using Entities.ServiceModels;

namespace Services.NhlData;

/// <summary>
/// Parses the NHL's plain-text roster report. Each team section starts with "Playing Roster - TEAM NAME",
/// then an "Active" list and an "Injury Reserve List" whose rows end with the date the player went on IR.
/// </summary>
public static partial class RosterReportParser
{
    // Optional sweater number (missing on some long-term IR rows), name, position, height, weight, birthdate,
    // birthplace, then an optional IR date separated by 2+ spaces
    [GeneratedRegex(@"^\s*(?:\d+)?\s+(?<name>\S.*?)\s+(?<pos>[GDCLR])\s+\d+'\d+""\s+\d+lbs\.\s+(?<born>\d{4}-\d{2}-\d{2})(?:\s+.*?)?(?:\s{2,}(?<ir>\d{4}-\d{2}-\d{2}))?\s*$")]
    private static partial Regex PlayerRow();

    [GeneratedRegex(@"^Last update:\s*(?<value>\d{2}:\d{2}:\d{2} \d{2}/\d{2}/\d{4})")]
    private static partial Regex LastUpdate();

    private const string TEAM_HEADER = "Playing Roster - ";

    public static List<ServiceRosterReportEntry> Parse(string report)
    {
        var entries = new List<ServiceRosterReportEntry>();
        string? team = null;
        DateTime? updated = null;
        bool? isInjuredReserve = null;

        foreach (var rawLine in report.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith(TEAM_HEADER))
            {
                team = line[TEAM_HEADER.Length..].Trim();
                updated = null;
                isInjuredReserve = null;
                continue;
            }

            var trimmed = line.Trim();
            var lastUpdate = LastUpdate().Match(trimmed);
            if (lastUpdate.Success)
            {
                updated = DateTime.ParseExact(lastUpdate.Groups["value"].Value, "HH:mm:ss MM/dd/yyyy", CultureInfo.InvariantCulture);
                continue;
            }
            if (trimmed == "Active")
            {
                isInjuredReserve = false;
                continue;
            }
            if (trimmed.StartsWith("Injury Reserve List"))
            {
                isInjuredReserve = true;
                continue;
            }
            // "Active - 23", "On IRL - 2", "Total - 25" close the section
            if (trimmed.StartsWith("Active - ") || trimmed.StartsWith("Total - "))
            {
                isInjuredReserve = null;
                continue;
            }

            if (team == null || isInjuredReserve == null)
                continue;

            var row = PlayerRow().Match(line);
            if (!row.Success)
                continue;

            var irDate = row.Groups["ir"].Success ? ParseDate(row.Groups["ir"].Value) : null;
            entries.Add(new ServiceRosterReportEntry(
                team,
                row.Groups["name"].Value.Trim(),
                row.Groups["pos"].Value[0],
                ParseDate(row.Groups["born"].Value)!.Value,
                isInjuredReserve.Value,
                irDate,
                updated));
        }

        // The report sometimes repeats a row verbatim
        return entries.Distinct().ToList();
    }

    private static DateTime? ParseDate(string value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}