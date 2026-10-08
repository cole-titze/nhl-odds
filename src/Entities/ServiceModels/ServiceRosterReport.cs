namespace Entities.ServiceModels;

/// <summary>
/// One player row of the NHL's public roster report (media.nhl.com/site/api/team/reports/roster/public).
/// The report is plain text with no player ids, so players are identified by name and birthdate.
/// </summary>
public record ServiceRosterReportEntry(
    string TeamName,
    string Name,
    char Position,
    DateTime BirthDate,
    bool IsInjuredReserve,
    DateTime? InjuredReserveDate,
    DateTime? ReportUpdated
);