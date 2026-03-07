import pandas as pd
import pytds

from .queries import CURRENT_SEASON_GAMES_QUERY, TEAM_NAMES_QUERY, TRAINING_DATA_QUERY, UNPLAYED_GAMES_QUERY


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


def load_team_names(conn: pytds.Connection) -> dict[int, str]:
    df = _query_to_dataframe(conn, TEAM_NAMES_QUERY)
    return dict(zip(df["TeamId"], df["Name"]))
