using Entities.Types.Enums;

namespace Entry;

public class ModeSettings
{
    public ModeType Mode { get; set; } = ModeType.NhlAdd;
    public string ConnectionString { get; set; } = string.Empty;
    public int ThrottleTimeMs { get; set; }
    public string OddsApiKey { get; set; } = string.Empty;
    public string OddsApiBackfillKey { get; set; } = string.Empty;
    public IEnumerable<int> BackfillGameIds { get; set; } = [];
    // NhlAdd re-fetches games played in this many past days to pick up NHL stat corrections; 0 turns it off
    public int RefetchDays { get; set; } = 7;
}