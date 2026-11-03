namespace WebApi;

public static class Seasons
{
    // Mirrors getCurrentSeason in frontend/src/utils/season.ts: a new season starts on September 20
    public static int Current(DateTime date) =>
        date >= new DateTime(date.Year, 9, 20) ? date.Year : date.Year - 1;
}
