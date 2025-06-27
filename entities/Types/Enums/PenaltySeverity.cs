namespace Entities.Types.Enums
{
    public enum PenaltySeverity
    {
        Minor,
        Major,
        Bench,
        Misconduct,
    }
    public static class PenaltySeverityParser
    {
        public static PenaltySeverity ParseFromString(string penaltySeverityType)
        {
            switch (penaltySeverityType)
            {
                case "MIN":
                    return PenaltySeverity.Minor;
                case "MAJ":
                    return PenaltySeverity.Major;
                case "BEN":
                    return PenaltySeverity.Bench;
                case "MIS":
                    return PenaltySeverity.Misconduct;
                default:
                    throw new ArgumentException($"Invalid PenaltySeverity value: {penaltySeverityType}", nameof(penaltySeverityType));
            }
        }
    }
}