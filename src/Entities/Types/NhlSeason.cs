namespace Entities.Types;

public static class NhlSeason
{
    /// <summary>
    /// The season a date belongs to on the site, as its start year: a new season starts on September 20.
    /// Mirrors getCurrentSeason in frontend/src/utils/season.ts.
    /// </summary>
    public static int Current(DateTime date) =>
        date >= new DateTime(date.Year, 9, 20) ? date.Year : date.Year - 1;
}
