using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

/// <summary>
/// One player's row from a snapshot of the NHL's public roster report: which team lists him and whether he is
/// on the Active list or the Injured Reserve list.
/// </summary>
public class DbRosterStatus
{
    public DateTime SnapshotUTC { get; set; }
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public bool IsInjuredReserve { get; set; }
    public DateTime? InjuredReserveDate { get; set; }
    // The report's "Last update" time for the team section, as printed (time zone not stated)
    public DateTime? ReportUpdated { get; set; }
    [ForeignKey(nameof(PlayerId))]
    public DbPlayer? Player { get; set; }
    [ForeignKey(nameof(TeamId))]
    public DbTeam? Team { get; set; }
}