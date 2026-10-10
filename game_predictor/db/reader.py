import time
from functools import lru_cache

import pandas as pd

from ..features.expected_goals import compute_xg_features
from .queries import (
    ALL_GAMES_QUERY,
    CONSENSUS_SPREAD_QUERY,
    CONSENSUS_TOTAL_QUERY,
    CURRENT_SEASON_GAMES_QUERY,
    SHOT_ATTEMPTS_QUERY,
    TEAM_NAMES_QUERY,
    TRAINING_DATA_QUERY,
    UNPLAYED_GAMES_QUERY,
)


def _query_to_dataframe(conn, query: str) -> pd.DataFrame:
    with conn.cursor() as cursor:
        cursor.execute(query)
        columns = [desc[0] for desc in cursor.description]
        rows = cursor.fetchall()
    return pd.DataFrame(rows, columns=columns)


@lru_cache(maxsize=1)
def _load_xg_features(conn) -> pd.DataFrame:
    """Expected-goals features for every game. Computed once per connection: it scores every shot attempt."""
    start = time.time()
    attempts = _query_to_dataframe(conn, SHOT_ATTEMPTS_QUERY)
    games = _query_to_dataframe(conn, ALL_GAMES_QUERY)
    features = compute_xg_features(attempts, games)
    print(f"Expected-goals features: {len(attempts):,} attempts in {time.time() - start:.0f}s")
    return features


def _load_games(conn, query: str) -> pd.DataFrame:
    """Load games with their GameCleaned features plus the expected-goals features."""
    df = _query_to_dataframe(conn, query)
    return df.join(_load_xg_features(conn), on="GameId")


def load_training_data(conn) -> pd.DataFrame:
    return _load_games(conn, TRAINING_DATA_QUERY)


def load_unplayed_games(conn) -> pd.DataFrame:
    return _load_games(conn, UNPLAYED_GAMES_QUERY)


def load_current_season_games(conn) -> pd.DataFrame:
    return _load_games(conn, CURRENT_SEASON_GAMES_QUERY)


def load_consensus_lines(conn) -> dict[int, dict[str, float]]:
    """Load each game's most common spread and total lines keyed by GameId.

    Returns {GameId: {"spread": home_handicap, "total": ou_point}}.
    """
    spread_df = _query_to_dataframe(conn, CONSENSUS_SPREAD_QUERY)
    total_df = _query_to_dataframe(conn, CONSENSUS_TOTAL_QUERY)

    lines: dict[int, dict[str, float]] = {}
    for _, row in spread_df.iterrows():
        lines.setdefault(int(row["GameId"]), {})["spread"] = float(row["ConsensusSpread"])
    for _, row in total_df.iterrows():
        lines.setdefault(int(row["GameId"]), {})["total"] = float(row["ConsensusTotal"])
    return lines


def load_team_names(conn) -> dict[int, str]:
    df = _query_to_dataframe(conn, TEAM_NAMES_QUERY)
    return dict(zip(df["TeamId"], df["Name"]))
