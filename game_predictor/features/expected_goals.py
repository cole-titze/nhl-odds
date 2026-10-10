"""Expected-goals team features (issue #107), computed from play-by-play shot attempts.

An xG model scores every unblocked attempt walk-forward (season s is scored by a model trained on earlier seasons),
then each team's attempts are rolled up over its last 10 and 82 games before puck drop. The features are home minus
away, named as in XG_FEATURE_COLUMNS.
"""

import lightgbm as lgb
import numpy as np
import pandas as pd

from ..db.queries import XG_FEATURE_COLUMNS

_XG_FEATS = ["dist", "angle", "behind_net", "ShotType", "rebound", "sk_diff", "own_sk", "opp_sk", "ot"]
_SUMS = ["cf5", "ca5", "xgf5", "xga5", "xgf", "xga", "gf", "ga", "xgf_pp", "xga_pk"]
_WINDOWS = (10, 82)


def _prepare_attempts(attempts: pd.DataFrame, games: pd.DataFrame) -> pd.DataFrame:
    """Add strength state, shot geometry and rebound flags; drop shootouts, penalty shots and bad situation codes."""
    a = attempts[(attempts.PeriodType != 2) & (attempts.SituationCode >= 0)].copy()
    sc = a.SituationCode.astype(int).astype(str).str.zfill(4)
    away_g, away_sk, home_sk, home_g = (sc.str[i].astype(int) for i in range(4))
    a = a.join(games[["SeasonStartYear", "HomeTeamId"]], on="GameId")
    a["is_home"] = a.TeamId == a.HomeTeamId
    a["own_sk"] = np.where(a.is_home, home_sk, away_sk)
    a["opp_sk"] = np.where(a.is_home, away_sk, home_sk)
    a["opp_g"] = np.where(a.is_home, away_g, home_g)
    a = a[~((a.own_sk == 1) & (a.opp_sk == 0))]  # penalty shots (0101 / 1010)
    a = a[(a.own_sk >= 3) & (a.opp_sk >= 3)]
    a["en"] = a.opp_g == 0

    # The home team attacks the end where its attempts cluster in each period
    h = np.where(a.is_home, 1, -1)
    home_sign = np.sign((h * a.XCoordinate).groupby([a.GameId, a.PeriodNumber]).transform("sum")).replace(0, 1)
    xn = a.XCoordinate * home_sign * h
    a["dist"] = np.hypot(89 - xn, a.YCoordinate)
    a["angle"] = np.degrees(np.arctan2(a.YCoordinate.abs(), 89 - xn))
    a["behind_net"] = (xn > 89).astype(int)

    # Rebound: the same team's previous attempt in the period came within 3 seconds
    a = a.sort_values(["GameId", "PeriodNumber", "SecondsIntoPeriod", "SortOrder"])
    prev_t = a.groupby(["GameId", "PeriodNumber"]).SecondsIntoPeriod.shift()
    prev_team = a.groupby(["GameId", "PeriodNumber"]).TeamId.shift()
    a["rebound"] = ((a.SecondsIntoPeriod - prev_t <= 3) & (prev_team == a.TeamId)).astype(int)
    a["sk_diff"] = a.own_sk - a.opp_sk
    a["ot"] = (a.PeriodType == 1).astype(int)
    a["state"] = np.select(
        [a.en, (a.own_sk == 5) & (a.opp_sk == 5), a.own_sk > a.opp_sk, a.own_sk < a.opp_sk],
        ["en", "ev5", "pp", "sh"],
        "ev_other",
    )
    return a


def _score_attempts(a: pd.DataFrame) -> pd.Series:
    """Walk-forward xG for every non-empty-net attempt; the first season is scored by a model trained on the second."""
    xg = pd.Series(np.nan, index=a.index)
    scored = ~a.en
    seasons = sorted(a.SeasonStartYear.unique())
    for season in seasons:
        train_on = (a.SeasonStartYear < season) if season != seasons[0] else (a.SeasonStartYear == seasons[1])
        train = a[scored & train_on]
        test_idx = a.index[scored & (a.SeasonStartYear == season)]
        model = lgb.LGBMClassifier(
            n_estimators=300,
            learning_rate=0.05,
            num_leaves=31,
            min_child_samples=200,
            subsample=0.8,
            subsample_freq=1,
            verbose=-1,
        )
        model.fit(train[_XG_FEATS], train.Goal, categorical_feature=["ShotType"])
        xg[test_idx] = model.predict_proba(a.loc[test_idx, _XG_FEATS])[:, 1]
    return xg


def _team_game_totals(a: pd.DataFrame, games: pd.DataFrame) -> pd.DataFrame:
    """Per played game and team: attempt and xG totals for and against, sorted by team and date."""
    b = a[~a.en]
    by_team = ["GameId", "TeamId"]
    totals = pd.DataFrame(
        {
            "cf5": b[b.state == "ev5"].groupby(by_team).size(),
            "xgf5": b[b.state == "ev5"].groupby(by_team).xg.sum(),
            "xgf": b.groupby(by_team).xg.sum(),
            "gf": b.groupby(by_team).Goal.sum(),
            "xgf_pp": b[b.state == "pp"].groupby(by_team).xg.sum(),
        }
    ).fillna(0)
    played = games[games.index.isin(a.GameId.unique())]
    pairs = pd.concat(
        [
            played[["HomeTeamId", "AwayTeamId"]].rename(columns={"HomeTeamId": "TeamId", "AwayTeamId": "OppId"}),
            played[["AwayTeamId", "HomeTeamId"]].rename(columns={"AwayTeamId": "TeamId", "HomeTeamId": "OppId"}),
        ]
    ).reset_index()
    pairs = pairs.join(totals, on=by_team).join(totals.add_suffix("_opp"), on=["GameId", "OppId"]).fillna(0)
    pairs = pairs.rename(
        columns={"cf5_opp": "ca5", "xgf5_opp": "xga5", "xgf_opp": "xga", "gf_opp": "ga", "xgf_pp_opp": "xga_pk"}
    )
    pairs = pairs.join(games[["GameDateUTC"]], on="GameId")
    return pairs.sort_values(["TeamId", "GameDateUTC", "GameId"]).reset_index(drop=True)


def _team_form(pairs: pd.DataFrame) -> pd.DataFrame:
    """Each team's rates over its last n games up to and including each played game."""
    form = pairs[["TeamId", "GameDateUTC"]].copy()
    for n in _WINDOWS:
        r = pairs.groupby("TeamId")[_SUMS].transform(lambda s: s.rolling(n, min_periods=1).sum())
        games_played = pairs.groupby("TeamId").cf5.transform(lambda s: s.rolling(n, min_periods=1).count())
        window = {
            "FenwickPct5v5": r.cf5 / (r.cf5 + r.ca5),
            "XgPct5v5": r.xgf5 / (r.xgf5 + r.xga5),
            "XgAvg": r.xgf / games_played,
            "ConcededXgAvg": r.xga / games_played,
            "GoalsSavedAboveXgAvg": (r.xga - r.ga) / games_played,
            "GoalsAboveXgAvg": (r.gf - r.xgf) / games_played,
            "PpXgAvg": r.xgf_pp / games_played,
            "PkConcededXgAvg": r.xga_pk / games_played,
        }
        for stat, values in window.items():
            form[f"Last{n}{stat}"] = values
    return form.sort_values("GameDateUTC")


def _form_before(games: pd.DataFrame, form: pd.DataFrame, team_col: str) -> pd.DataFrame:
    """Each game's team form as of its last played game before puck drop, so scheduled games get values too."""
    left = (
        games[["GameDateUTC", team_col]].rename(columns={team_col: "TeamId"}).reset_index().sort_values("GameDateUTC")
    )
    merged = pd.merge_asof(left, form, on="GameDateUTC", by="TeamId", allow_exact_matches=False)
    return merged.set_index("GameId").drop(columns=["GameDateUTC", "TeamId"])


def compute_xg_features(attempts: pd.DataFrame, games: pd.DataFrame) -> pd.DataFrame:
    """XG_FEATURE_COLUMNS for every game in `games` (played or not), indexed by GameId.

    Teams without prior games get the median over played regular-season games.
    """
    games = games.set_index("GameId")
    a = _prepare_attempts(attempts, games)
    a["xg"] = _score_attempts(a)
    form = _team_form(_team_game_totals(a, games))

    home = _form_before(games, form, "HomeTeamId")
    away = _form_before(games, form, "AwayTeamId").reindex(home.index)
    features = pd.DataFrame({f"{col}Advantage": home[col] - away[col] for col in home.columns})[XG_FEATURE_COLUMNS]

    training_games = games.index[(games.GameType == 2) & games.HasBeenPlayed]
    return features.fillna(features.loc[features.index.intersection(training_games)].median())
