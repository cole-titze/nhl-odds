namespace Entities.Types.Enums
{
    public enum PenaltySeverity
    {
        Minor,
        Major,
        Bench,
    }
    public static class PenaltySeverityParser
    {
        public static PenaltySeverity ParseFromString(string periodType)
        {
            switch (periodType)
            {
                case "MIN":
                    return PenaltySeverity.Minor;
                case "MAJ":
                    return PenaltySeverity.Major;
                case "BEN":
                    return PenaltySeverity.Bench;
                default:
                    throw new ArgumentException($"Invalid PeriodType value: {periodType}", nameof(periodType));
            }
        }
    }
}