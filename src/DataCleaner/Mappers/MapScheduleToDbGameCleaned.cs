using Entities.DbModels;
using Entities.Models;
using Entities.Types.Enums;

namespace DataCleaner.Mappers;

/// <summary>
/// Travel, time-zone and last-season features from the schedule. Travel and trips count from each team's first game of
/// the season (which starts from its home city); last season's stats come from the previous regular season.
/// Differences are away minus home for travel and rest, home minus away for strength, like the other GameCleaned columns.
/// </summary>
public static class MapScheduleToDbGameCleaned
{
    // Venue city -> latitude, longitude and standard UTC offset (DST is ignored; only differences between cities are used).
    // Upcoming games have no venue yet and a city missing here (a new neutral site) counts as the home team's city.
    private static readonly Dictionary<string, (double Lat, double Lon, int UtcOffset)> Cities = new()
    {
        ["Anaheim"] = (33.81, -117.88, -8), ["Ann Arbor"] = (42.27, -83.75, -5), ["Annapolis"] = (38.98, -76.49, -5),
        ["Atlanta"] = (33.76, -84.40, -5), ["Berlin"] = (52.52, 13.40, 1), ["Boston"] = (42.37, -71.06, -5),
        ["Bronx"] = (40.83, -73.93, -5), ["Brooklyn"] = (40.68, -73.98, -5), ["Buffalo"] = (42.88, -78.88, -5),
        ["Calgary"] = (51.04, -114.05, -7), ["Chicago"] = (41.88, -87.67, -6), ["Colorado Springs"] = (38.83, -104.82, -7),
        ["Columbus"] = (39.97, -83.01, -5), ["Dallas"] = (32.79, -96.81, -6), ["Denver"] = (39.75, -105.01, -7),
        ["Detroit"] = (42.34, -83.06, -5), ["East Rutherford"] = (40.81, -74.07, -5), ["Edmonton"] = (53.55, -113.49, -7),
        ["Elmont"] = (40.71, -73.73, -5), ["Foxborough"] = (42.09, -71.26, -5), ["Glendale"] = (33.53, -112.26, -7),
        ["Gothenburg"] = (57.71, 11.97, 1), ["Hamilton"] = (43.26, -79.87, -5), ["Helsinki"] = (60.17, 24.94, 2),
        ["Los Angeles"] = (34.04, -118.27, -8), ["Miami"] = (25.78, -80.22, -5), ["Minneapolis"] = (44.97, -93.26, -6),
        ["Montreal"] = (45.50, -73.57, -5), ["Nashville"] = (36.16, -86.78, -6), ["New York"] = (40.75, -73.99, -5),
        ["Newark"] = (40.73, -74.17, -5), ["Notre Dame"] = (41.70, -86.24, -5), ["Ottawa"] = (45.30, -75.93, -5),
        ["Paradise"] = (36.10, -115.18, -8), ["Philadelphia"] = (39.90, -75.17, -5), ["Pittsburgh"] = (40.44, -79.99, -5),
        ["Prague"] = (50.08, 14.44, 1), ["Queens"] = (40.76, -73.85, -5), ["Raleigh"] = (35.80, -78.72, -5),
        ["Regina"] = (50.45, -104.61, -6), ["Salt Lake City"] = (40.77, -111.90, -7), ["San Jose"] = (37.33, -121.90, -8),
        ["Santa Clara"] = (37.40, -121.97, -8), ["Seattle"] = (47.62, -122.35, -8), ["St. Louis"] = (38.63, -90.20, -6),
        ["St. Paul"] = (44.94, -93.10, -6), ["Stateline"] = (38.96, -119.94, -8), ["Stockholm"] = (59.33, 18.07, 1),
        ["Sunrise"] = (26.16, -80.33, -5), ["Tampa"] = (27.94, -82.45, -5), ["Tampere"] = (61.50, 23.79, 2),
        ["Tempe"] = (33.42, -111.93, -7), ["Toronto"] = (43.64, -79.38, -5), ["Uniondale"] = (40.72, -73.59, -5),
        ["Vancouver"] = (49.28, -123.11, -8), ["Washington"] = (38.90, -77.02, -5), ["Winnipeg"] = (49.89, -97.14, -6),
    };

    // Relocated teams take their predecessor's last season (Atlanta -> Winnipeg, Phoenix -> Arizona -> Utah)
    private static readonly Dictionary<int, int> Predecessors = new() { [52] = 11, [53] = 27, [59] = 53, [68] = 59 };

    // BlendGoalDiff counts last season's goal difference as this many games, at two thirds strength (regressed to the mean)
    private const int PRIOR_GAMES = 20;
    private const double LAST_SEASON_WEIGHT = 2.0 / 3;

    private record TeamTravel(double Km, double Km7d, int Games7d, int TripLength, int TimeZoneShift, double BodyClockHour);

    private record LastSeason(double GoalDiff, double PointsPct);

    public static void Apply(DbGameCleaned cleanedGame, Game game, SeasonGames seasonGames)
    {
        var home = GetTeamTravel(game, game.HomeTeamId, seasonGames);
        var away = GetTeamTravel(game, game.AwayTeamId, seasonGames);
        if (home != null && away != null)
        {
            cleanedGame.TravelKmAdvantage = away.Km - home.Km;
            cleanedGame.Km7dAdvantage = away.Km7d - home.Km7d;
            cleanedGame.Games7dAdvantage = away.Games7d - home.Games7d;
            cleanedGame.HomeTripLength = home.TripLength;
            cleanedGame.AwayTripLength = away.TripLength;
            cleanedGame.AwayTimeZoneShift = away.TimeZoneShift;
            cleanedGame.AwayBodyClockStartHour = away.BodyClockHour;
        }

        var homeLast = GetLastSeason(game.HomeTeamId, game.SeasonStartYear, seasonGames);
        var awayLast = GetLastSeason(game.AwayTeamId, game.SeasonStartYear, seasonGames);
        if (homeLast != null && awayLast != null)
        {
            cleanedGame.PrevGoalDiffAdvantage = homeLast.GoalDiff - awayLast.GoalDiff;
            cleanedGame.PrevPointsPctAdvantage = homeLast.PointsPct - awayLast.PointsPct;
        }

        var homePlayed = PlayedThisSeasonBefore(game, game.HomeTeamId, seasonGames);
        var awayPlayed = PlayedThisSeasonBefore(game, game.AwayTeamId, seasonGames);
        cleanedGame.BlendGoalDiffAdvantage = BlendGoalDiff(homePlayed, game.HomeTeamId, homeLast)
            - BlendGoalDiff(awayPlayed, game.AwayTeamId, awayLast);
        cleanedGame.MinGamesPlayed = Math.Min(homePlayed.Count, awayPlayed.Count);
    }

    public static double DistanceKm(string from, string to)
    {
        var (lat1, lon1, _) = Cities[from];
        var (lat2, lon2, _) = Cities[to];
        static double Rad(double degrees) => degrees * Math.PI / 180;
        var h = Math.Pow(Math.Sin(Rad(lat2 - lat1) / 2), 2)
            + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Pow(Math.Sin(Rad(lon2 - lon1) / 2), 2);
        return 6371 * 2 * Math.Asin(Math.Sqrt(h));
    }

    private static TeamTravel? GetTeamTravel(Game game, int teamId, SeasonGames seasonGames)
    {
        var homeCity = GetHomeCity(teamId, game.SeasonStartYear, seasonGames);
        var venue = GetVenue(game, seasonGames);
        if (homeCity == null || venue == null)
            return null;

        // This season's earlier games, latest first (scheduled ones too: travel is known before they're played)
        var prior = TeamSeasonGames(teamId, game.SeasonStartYear, seasonGames)
            .Where(g => g.GameDateUTC < game.GameDateUTC)
            .ToList();

        // A game's trip starts from the previous game's venue, or home for the season's first game
        string? Origin(int priorIndex) => priorIndex < prior.Count ? GetVenue(prior[priorIndex], seasonGames) : homeCity;
        var km = DistanceKm(Origin(0) ?? homeCity, venue);

        var km7d = km;
        var games7d = 0;
        for (var i = 0; i < prior.Count && game.GameDateUTC - prior[i].GameDateUTC <= TimeSpan.FromDays(7); i++)
        {
            var priorVenue = GetVenue(prior[i], seasonGames) ?? homeCity;
            km7d += DistanceKm(Origin(i + 1) ?? homeCity, priorVenue);
            games7d++;
        }

        var isHome = game.HomeTeamId == teamId;
        var tripLength = 1 + prior.TakeWhile(g => (g.HomeTeamId == teamId) == isHome).Count();

        var homeOffset = Cities[homeCity].UtcOffset;
        var bodyClockHour = ((game.GameDateUTC.Hour + game.GameDateUTC.Minute / 60.0 + homeOffset) % 24 + 24) % 24;

        return new TeamTravel(km, km7d, games7d, tripLength, Cities[venue].UtcOffset - homeOffset, bodyClockHour);
    }

    /// <summary>Where the game is played: its venue city, else (upcoming games, unknown cities) the home team's city.</summary>
    private static string? GetVenue(Game game, SeasonGames seasonGames)
    {
        var city = game.ExtendedInfo?.VenueLocation.Trim();
        if (city != null && Cities.ContainsKey(city))
            return city;
        return GetHomeCity(game.HomeTeamId, game.SeasonStartYear, seasonGames);
    }

    /// <summary>A team's most common known venue in its home games this season, else last season.</summary>
    private static string? GetHomeCity(int teamId, int seasonStartYear, SeasonGames seasonGames)
    {
        if (!seasonGames.GamesMap.TryGetValue(teamId, out var teamGames))
            return null;
        foreach (var season in new[] { seasonStartYear, seasonStartYear - 1 })
        {
            var city = teamGames.HomeGames
                .Where(g => g.SeasonStartYear == season)
                .Select(g => g.ExtendedInfo?.VenueLocation.Trim())
                .Where(c => c != null && Cities.ContainsKey(c))
                .GroupBy(c => c!)
                .OrderByDescending(grp => grp.Count())
                .ThenBy(grp => grp.Key, StringComparer.Ordinal)
                .Select(grp => grp.Key)
                .FirstOrDefault();
            if (city != null)
                return city;
        }
        return null;
    }

    private static IEnumerable<Game> TeamSeasonGames(int teamId, int seasonStartYear, SeasonGames seasonGames) =>
        seasonGames.GamesMap.TryGetValue(teamId, out var teamGames)
            ? teamGames.Games.Where(g => g.SeasonStartYear == seasonStartYear)
            : [];

    private static List<Game> PlayedThisSeasonBefore(Game game, int teamId, SeasonGames seasonGames) =>
        TeamSeasonGames(teamId, game.SeasonStartYear, seasonGames)
            .Where(g => g.HasBeenPlayed && g.GameDateUTC < game.GameDateUTC)
            .ToList();

    private static LastSeason? GetLastSeason(int teamId, int seasonStartYear, SeasonGames seasonGames)
    {
        foreach (var id in Predecessors.TryGetValue(teamId, out var predecessor) ? new[] { teamId, predecessor } : [teamId])
        {
            var played = TeamSeasonGames(id, seasonStartYear - 1, seasonGames).Where(g => g.HasBeenPlayed).ToList();
            if (played.Count == 0)
                continue;
            var points = played.Sum(g => g.IsWinner(id) ? 2 : g.EndPeriod != PeriodType.Regulation ? 1 : 0);
            return new LastSeason(MapGameToDbGameCleaned.GetGoalDiffAvg(played, id), points / (2.0 * played.Count));
        }
        return null;
    }

    private static double BlendGoalDiff(List<Game> played, int teamId, LastSeason? lastSeason)
    {
        var current = MapGameToDbGameCleaned.GetGoalDiffAvg(played, teamId);
        var prior = (lastSeason?.GoalDiff ?? 0) * LAST_SEASON_WEIGHT;
        return (played.Count * current + PRIOR_GAMES * prior) / (played.Count + PRIOR_GAMES);
    }
}
