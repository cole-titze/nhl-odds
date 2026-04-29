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