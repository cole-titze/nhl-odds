"""Explains each saved win prediction as its main factors (#155): how much each feature group
(explain/factors.py) moved the home team's win probability, saved to GamePredictionFactor."""

import numpy as np
import pandas as pd

from ..db.queries import FEATURE_COLUMNS
from .factors import FEATURE_GROUPS, describe
from .shapley import group_shapley

# Background games the factors are measured against: a sample of the latest seasons' games
BACKGROUND_SIZE = 50
BACKGROUND_SEASONS = 2


def background_sample(train_df: pd.DataFrame, seed: int = 0) -> np.ndarray:
    latest = train_df["SeasonStartYear"].max()
    recent = train_df[train_df["SeasonStartYear"] > latest - BACKGROUND_SEASONS]
    sample = recent.sample(n=min(BACKGROUND_SIZE, len(recent)), random_state=seed)
    return sample[FEATURE_COLUMNS].to_numpy(dtype=float)


def explain_games(games_df: pd.DataFrame, pipeline, model, background: np.ndarray) -> list[dict]:
    """One row per game and factor group, ranked by size within each game (1 = largest).
    Contribution is the change in the home team's win probability (positive favors home)."""

    def predict_home(X: np.ndarray) -> np.ndarray:
        return model.predict_proba(pipeline.transform(X))[:, 0]

    names = list(FEATURE_GROUPS)
    X = games_df[FEATURE_COLUMNS].to_numpy(dtype=float)
    values, base = group_shapley(predict_home, X, background, [FEATURE_GROUPS[n] for n in names])

    rows = []
    for (_, game), game_values in zip(games_df.iterrows(), values):
        feature_row = {col: float(game[col]) for col in FEATURE_COLUMNS}
        order = np.argsort(-np.abs(game_values))
        for rank, g in enumerate(order, start=1):
            rows.append(
                {
                    "GameId": int(game["GameId"]),
                    "Rank": rank,
                    "Factor": names[g],
                    "Detail": describe(names[g], feature_row),
                    "Contribution": float(game_values[g]),
                    "BaseHomeOdds": base,
                }
            )
    return rows
