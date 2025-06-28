using Entities.Types.Enums;

public class ModeSettings
{
    public ModeType mode { get; set; } = ModeType.Add;
    public string connectionString { get; set; } = string.Empty;
}