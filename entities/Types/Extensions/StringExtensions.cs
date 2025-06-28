namespace Entities.Types;

public static class ToiExtensions
{
    public static int ParseIceTimeToSeconds(this string toi)
    {
        if (string.IsNullOrWhiteSpace(toi))
            throw new ArgumentException("TOI string is null or empty");

        var parts = toi.Split(':');
        if (parts.Length != 2)
            throw new FormatException("TOI must be in MM:SS format");

        if (!int.TryParse(parts[0], out int minutes) || !int.TryParse(parts[1], out int seconds))
            throw new FormatException("TOI contains non-numeric values");

        return (minutes * 60) + seconds;
    }
}
public static class SituationCodeParser
{
    public static int ParseFromString(string situationCode)
    {
        if (string.IsNullOrWhiteSpace(situationCode))
            return -1;

        if (int.TryParse(situationCode, out int result))
            return result;

        throw new FormatException("situationCode is not a valid integer: " + situationCode);
    }
}