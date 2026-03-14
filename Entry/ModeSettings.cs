using Entities.Types.Enums;

namespace Entry;

public class ModeSettings
{
    public ModeType Mode { get; set; } = ModeType.Add;
    public string ConnectionString { get; set; } = string.Empty;
    public int ThrottleTimeMs { get; set; }
    public string OddsApiKey { get; set; } = string.Empty;
}