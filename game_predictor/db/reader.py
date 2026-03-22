import pandas as pd
import pytds

from .queries import (
    CONSENSUS_SPREAD_QUERY,
    CONSENSUS_TOTAL_QUERY,
    CURRENT_SEASON_GAMES_QUERY,
    TEAM_NAMES_QUERY,
    TRAINING_DATA_QUERY,
    UNPLAYED_GAMES_QUERY,
)


def _query_to_dataframe(conn: pytds.Connection, query: str) -> pd.DataFrame:
    with conn.cursor() as cursor:
        cursor.execute(query)
        columns = [desc[0] for desc in cursor.description]
        rows = cursor.fetchall()
    return pd.DataFrame(rows, columns=columns)


def load_training_data(conn: pytds.Connection) -> pd.DataFrame:
    return _query_to_dataframe(conn, TRAINING_DATA_QUERY)


def load_unplayed_games(conn: pytds.Connection) -> pd.DataFrame:
    return _query_to_dataframe(conn, UNPLAYED_GAMES_QUERY)


def load_current_season_games(conn: pytds.Connection) -> pd.DataFrame:
    return _query_to_dataframe(conn, CURRENT_SEASON_GAMES_QUERY)


def load_consensus_lines(conn: pytds.Connection) -> dict[int, dict[str, float]]:
    """Load consensus spread and total lines keyed by GameId.

    Returns {GameId: {"spread": avg_home_spread, "total": avg_ou_point}}.
    """
    spread_df = _query_to_dataframe(conn, CONSENSUS_SPREAD_QUERY)
    total_df = _query_to_dataframe(conn, CONSENSUS_TOTAL_QUERY)

    lines: dict[int, dict[str, float]] = {}
    for _, row in spread_df.iterrows():
        lines.setdefault(int(row["GameId"]), {})["spread"] = float(row["ConsensusSpread"])
    for _, row in total_df.iterrows():
        lines.setdefault(int(row["GameId"]), {})["total"] = float(row["ConsensusTotal"])
    return lines


def load_team_names(conn: pytds.Connection) -> dict[int, str]:
    df = _query_to_dataframe(conn, TEAM_NAMES_QUERY)
    return dict(zip(df["TeamId"], df["Name"]))
