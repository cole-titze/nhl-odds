namespace Entities.Types.Enums;

public enum PeriodType
{
    Regulation,
    Overtime,
    Shootout
}
public static class PeriodTypeParser
{
    public static PeriodType ParseFromString(string periodType)
    {
        switch (periodType)
        {
            case "REG":
                return PeriodType.Regulation;
            case "OT":
                return PeriodType.Overtime;
            case "SO":
                return PeriodType.Shootout;
            default:
                throw new ArgumentException($"Invalid PeriodType value: {periodType}", nameof(periodType));
        }
    }
}