namespace Entities.Types.Enums;

public enum ModeType
{
    Add,
    Update
}
public static class ModeTypeParser
{
    public static ModeType ParseFromString(string? modeType)
    {
        switch (modeType)
        {
            case "Update":
                return ModeType.Update;
            case "Add":
            case null:
                return ModeType.Add;
            default:
                throw new ArgumentException($"Invalid ModeType value: {modeType}", nameof(modeType));
        }
    }
}