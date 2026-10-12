"""Margin-of-victory Elo team ratings, carried from season to season.

Every regular season and playoff game since 2009 updates the ratings in order, so they live in the predictor rather
than the C# cleaner, which only sees a season and the one before it. Scheduled games get each team's rating as of its
last played game. Settings (ELO) are in prediction/experiments/features.py; columns are ELO_FEATURE_COLUMNS.
"""

import numpy as np
import pandas as pd

from ..db.queries import ELO_FEATURE_COLUMNS
from ..prediction.experiments.features import ELO

# Relocated teams keep their predecessor's rating (Atlanta -> Winnipeg, Phoenix -> Arizona -> Utah)
_PREDECESSOR = {52: 11, 53: 27, 59: 53, 68: 59}
_MEAN = 1500.0


def compute_elo_features(games: pd.DataFrame) -> pd.DataFrame:
    """ELO_FEATURE_COLUMNS for every regular season and playoff game in `games` (played or not), indexed by GameId."""
    games = games[games.GameType.isin([2, 3])].copy()
    games["GameDateUTC"] = pd.to_datetime(games.GameDateUTC, utc=True)
    games = games.sort_values(["GameDateUTC", "GameId"])

    k, home_advantage = ELO["k"], ELO["home_advantage"]
    elo, season_of, pre_game = {}, {}, {}
    for r in games.itertuples():
        for team in (r.HomeTeamId, r.AwayTeamId):
            if season_of.get(team) != r.SeasonStartYear:
                start = elo.get(team, elo.get(_PREDECESSOR.get(team), _MEAN))
                elo[team] = _MEAN + (1 - ELO["season_regression"]) * (start - _MEAN)
                season_of[team] = r.SeasonStartYear
        home, away = elo[r.HomeTeamId], elo[r.AwayTeamId]
        pre_game[r.GameId] = (home, away, home - away)
        if not r.HasBeenPlayed:
            continue

        edge = home + home_advantage - away
        won = float(r.HomeGoals > r.AwayGoals)
        # FiveThirtyEight's margin multiplier: bigger wins count more, less so when the favorite wins
        margin = np.log(abs(r.HomeGoals - r.AwayGoals) + 1) * 2.2 / ((edge if won else -edge) * 0.001 + 2.2)
        overtime = ELO["overtime_weight"] if r.EndPeriod > 0 else 1.0  # EndPeriod: 0 regulation, 1 OT, 2 shootout
        delta = k * margin * overtime * (won - 1 / (1 + 10 ** (-edge / 400)))
        elo[r.HomeTeamId] += delta
        elo[r.AwayTeamId] -= delta

    return pd.DataFrame.from_dict(pre_game, orient="index", columns=ELO_FEATURE_COLUMNS).rename_axis("GameId")
