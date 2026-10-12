"""Schedule team features: travel, time zones and an early-season carry-over of last season's strength.

Everything comes from GameRaw (dates, venues, scores), so scheduled games get values too. Columns are named as in
SCHEDULE_FEATURE_COLUMNS.
"""

from math import asin, cos, radians, sin, sqrt

import numpy as np
import pandas as pd

from ..db.queries import SCHEDULE_FEATURE_COLUMNS

# Venue city -> (latitude, longitude, standard UTC offset). DST is ignored; only differences between cities are used.
# A city missing here (a new neutral site) is treated as the home team's city.
_CITIES = {
    "Anaheim": (33.81, -117.88, -8),
    "Ann Arbor": (42.27, -83.75, -5),
    "Annapolis": (38.98, -76.49, -5),
    "Atlanta": (33.76, -84.40, -5),
    "Berlin": (52.52, 13.40, 1),
    "Boston": (42.37, -71.06, -5),
    "Bronx": (40.83, -73.93, -5),
    "Brooklyn": (40.68, -73.98, -5),
    "Buffalo": (42.88, -78.88, -5),
    "Calgary": (51.04, -114.05, -7),
    "Chicago": (41.88, -87.67, -6),
    "Colorado Springs": (38.83, -104.82, -7),
    "Columbus": (39.97, -83.01, -5),
    "Dallas": (32.79, -96.81, -6),
    "Denver": (39.75, -105.01, -7),
    "Detroit": (42.34, -83.06, -5),
    "East Rutherford": (40.81, -74.07, -5),
    "Edmonton": (53.55, -113.49, -7),
    "Elmont": (40.71, -73.73, -5),
    "Foxborough": (42.09, -71.26, -5),
    "Glendale": (33.53, -112.26, -7),
    "Gothenburg": (57.71, 11.97, 1),
    "Hamilton": (43.26, -79.87, -5),
    "Helsinki": (60.17, 24.94, 2),
    "Los Angeles": (34.04, -118.27, -8),
    "Miami": (25.78, -80.22, -5),
    "Minneapolis": (44.97, -93.26, -6),
    "Montreal": (45.50, -73.57, -5),
    "Nashville": (36.16, -86.78, -6),
    "New York": (40.75, -73.99, -5),
    "Newark": (40.73, -74.17, -5),
    "Notre Dame": (41.70, -86.24, -5),
    "Ottawa": (45.30, -75.93, -5),
    "Paradise": (36.10, -115.18, -8),
    "Philadelphia": (39.90, -75.17, -5),
    "Pittsburgh": (40.44, -79.99, -5),
    "Prague": (50.08, 14.44, 1),
    "Queens": (40.76, -73.85, -5),
    "Raleigh": (35.80, -78.72, -5),
    "Regina": (50.45, -104.61, -6),
    "Salt Lake City": (40.77, -111.90, -7),
    "San Jose": (37.33, -121.90, -8),
    "Santa Clara": (37.40, -121.97, -8),
    "Seattle": (47.62, -122.35, -8),
    "St. Louis": (38.63, -90.20, -6),
    "St. Paul": (44.94, -93.10, -6),
    "Stateline": (38.96, -119.94, -8),
    "Stockholm": (59.33, 18.07, 1),
    "Sunrise": (26.16, -80.33, -5),
    "Tampa": (27.94, -82.45, -5),
    "Tampere": (61.50, 23.79, 2),
    "Tempe": (33.42, -111.93, -7),
    "Toronto": (43.64, -79.38, -5),
    "Uniondale": (40.72, -73.59, -5),
    "Vancouver": (49.28, -123.11, -8),
    "Washington": (38.90, -77.02, -5),
    "Winnipeg": (49.89, -97.14, -6),
}

# Relocated teams carry their predecessor's Elo and last season (Atlanta -> Winnipeg, Phoenix -> Arizona -> Utah)
_PREDECESSOR = {52: 11, 53: 27, 59: 53, 68: 59}

_ELO_K, _ELO_HOME, _ELO_REGRESS = 6.0, 35.0, 1 / 3
_PRIOR_GAMES = 20  # last season's goal difference counts as this many games in BlendGoalDiff


def _km(a: str, b: str) -> float:
    (la1, lo1, _), (la2, lo2, _) = _CITIES[a], _CITIES[b]
    la1, lo1, la2, lo2 = map(radians, (la1, lo1, la2, lo2))
    h = sin((la2 - la1) / 2) ** 2 + cos(la1) * cos(la2) * sin((lo2 - lo1) / 2) ** 2
    return 6371 * 2 * asin(sqrt(h))


def _venues(games: pd.DataFrame) -> tuple[pd.Series, dict]:
    """Each game's venue city, and a lookup of each team's home city by (season, team).

    Scheduled games have no venue yet and take the home team's city. A team's home city is its most common known
    venue that season, else its latest one.
    """
    known = games[games.VenueLocation.isin(_CITIES)]
    by_season = known.groupby(["SeasonStartYear", "HomeTeamId"]).VenueLocation.agg(lambda s: s.mode().iloc[0])
    latest = known.groupby("HomeTeamId").apply(
        lambda g: g[g.SeasonStartYear == g.SeasonStartYear.max()].VenueLocation.mode().iloc[0], include_groups=False
    )

    def home_city(season, team):
        return by_season.get((season, team), latest.get(team))

    venue = [
        v if v in _CITIES else home_city(s, h)
        for v, s, h in zip(games.VenueLocation, games.SeasonStartYear, games.HomeTeamId)
    ]
    return pd.Series(venue, index=games.index), home_city


def _travel(games: pd.DataFrame) -> pd.DataFrame:
    venue, home_city = _venues(games)
    long = pd.concat(
        [
            games.assign(TeamId=games.HomeTeamId, is_home=True, venue=venue),
            games.assign(TeamId=games.AwayTeamId, is_home=False, venue=venue),
        ]
    ).sort_values(["GameDateUTC", "GameId"])

    rows = []
    for (season, team), g in long.groupby(["SeasonStartYear", "TeamId"], sort=False):
        home = home_city(season, team)
        prev_city, prev_home, run, recent = home, None, 0, []  # recent: (puck drop, km) of earlier games
        for r in g.itertuples():
            if r.venue is None or home is None:
                rows.append({"GameId": r.GameId, "is_home": r.is_home})
                continue
            km = _km(prev_city, r.venue)
            run = run + 1 if r.is_home == prev_home else 1
            recent = [x for x in recent if r.GameDateUTC - x[0] <= pd.Timedelta(days=7)]
            tz = _CITIES[r.venue][2] - _CITIES[home][2]
            rows.append(
                {
                    "GameId": r.GameId,
                    "is_home": r.is_home,
                    "km": km,
                    "km7d": km + sum(x[1] for x in recent),
                    "games7d": len(recent),
                    "trip": run,
                    "tz": tz,
                    "body_hour": (r.GameDateUTC.hour + r.GameDateUTC.minute / 60 + _CITIES[home][2]) % 24,
                }
            )
            recent.append((r.GameDateUTC, km))
            prev_city, prev_home = r.venue, r.is_home
    t = pd.DataFrame(rows)
    h = t[t.is_home].set_index("GameId")
    a = t[~t.is_home].set_index("GameId").reindex(h.index)
    return pd.DataFrame(
        {
            "TravelKmAdvantage": a.km - h.km,
            "Km7dAdvantage": a.km7d - h.km7d,
            "Games7dAdvantage": a.games7d - h.games7d,
            "HomeTripLength": h.trip,
            "AwayTripLength": a.trip,
            "AwayTimeZoneShift": a.tz,
            "AwayBodyClockStartHour": a.body_hour,
        }
    )


def _elo(games: pd.DataFrame) -> pd.DataFrame:
    """Pre-game margin-of-victory Elo over regular season and playoff games."""
    elo, season_of, pre = {}, {}, {}
    for r in games.itertuples():
        for team in (r.HomeTeamId, r.AwayTeamId):
            if season_of.get(team) != r.SeasonStartYear:
                start = elo.get(team, elo.get(_PREDECESSOR.get(team), 1500.0))
                elo[team] = 1500 + (1 - _ELO_REGRESS) * (start - 1500)
                season_of[team] = r.SeasonStartYear
        home, away = elo[r.HomeTeamId], elo[r.AwayTeamId]
        pre[r.GameId] = (home, away)
        if not r.HasBeenPlayed:
            continue
        edge = home + _ELO_HOME - away
        won = float(r.HomeGoals > r.AwayGoals)
        margin = np.log(abs(r.HomeGoals - r.AwayGoals) + 1) * 2.2 / ((edge if won else -edge) * 0.001 + 2.2)
        delta = _ELO_K * margin * (won - 1 / (1 + 10 ** (-edge / 400))) * (0.75 if r.EndPeriod > 3 else 1)
        elo[r.HomeTeamId] += delta
        elo[r.AwayTeamId] -= delta
    e = pd.DataFrame.from_dict(pre, orient="index", columns=["HomeElo", "AwayElo"])
    e["EloAdvantage"] = e.HomeElo - e.AwayElo
    return e


def _carry_over(games: pd.DataFrame) -> pd.DataFrame:
    """Last regular season's goal difference and points percentage, and this season's goal difference shrunk to it."""
    reg = games[games.GameType == 2]
    long = pd.concat(
        [
            reg.assign(
                TeamId=reg.HomeTeamId, side="Home", gd=reg.HomeGoals - reg.AwayGoals, won=reg.HomeGoals > reg.AwayGoals
            ),
            reg.assign(
                TeamId=reg.AwayTeamId, side="Away", gd=reg.AwayGoals - reg.HomeGoals, won=reg.AwayGoals > reg.HomeGoals
            ),
        ]
    ).sort_values(["GameDateUTC", "GameId"])
    long["gd"] = long.gd.where(long.HasBeenPlayed)
    long["pts"] = np.where(long.won, 2, np.where(long.EndPeriod > 3, 1, 0))
    played = long[long.HasBeenPlayed]
    season = played.groupby(["SeasonStartYear", "TeamId"]).agg(gd=("gd", "mean"), pp=("pts", "mean"))
    season["pp"] /= 2

    def last_season(s, team, col):
        for t in (team, _PREDECESSOR.get(team)):
            if (s - 1, t) in season.index:
                return season.at[(s - 1, t), col]
        return np.nan

    long["prev_gd"] = [last_season(s, t, "gd") for s, t in zip(long.SeasonStartYear, long.TeamId)]
    long["prev_pp"] = [last_season(s, t, "pp") for s, t in zip(long.SeasonStartYear, long.TeamId)]
    by_team = long.groupby(["SeasonStartYear", "TeamId"]).gd
    long["gp"] = by_team.transform(lambda s: s.notna().cumsum().shift(fill_value=0))
    cur = by_team.transform(lambda s: s.shift().expanding().mean()).fillna(0)
    # last season counts at two thirds, the same regression to the mean as Elo
    long["blend"] = (long.gp * cur + _PRIOR_GAMES * long.prev_gd.fillna(0) * 2 / 3) / (long.gp + _PRIOR_GAMES)

    w = long.pivot_table(index="GameId", columns="side", values=["prev_gd", "prev_pp", "blend", "gp"], dropna=False)
    return pd.DataFrame(
        {
            "PrevGoalDiffAdvantage": w.prev_gd.Home - w.prev_gd.Away,
            "PrevPointsPctAdvantage": w.prev_pp.Home - w.prev_pp.Away,
            "BlendGoalDiffAdvantage": w.blend.Home - w.blend.Away,
            "MinGamesPlayed": w.gp.min(axis=1),
        }
    )


def compute_schedule_features(games: pd.DataFrame) -> pd.DataFrame:
    """SCHEDULE_FEATURE_COLUMNS for every regular season and playoff game in `games` (played or not), by GameId.

    Missing values (first season, expansion teams) get the median over played regular-season games.
    """
    games = games[games.GameType.isin([2, 3])].copy()
    games["GameDateUTC"] = pd.to_datetime(games.GameDateUTC, utc=True)
    games["VenueLocation"] = games.VenueLocation.fillna("").str.strip()
    games["EndPeriod"] = games.EndPeriod.fillna(3)
    games = games.sort_values(["GameDateUTC", "GameId"]).reset_index(drop=True)

    features = _travel(games).join(_elo(games), how="outer").join(_carry_over(games), how="outer")
    features = features.reindex(columns=SCHEDULE_FEATURE_COLUMNS).astype(float)
    training_games = games.GameId[(games.GameType == 2) & games.HasBeenPlayed]
    return features.fillna(features.loc[features.index.intersection(training_games)].median())
