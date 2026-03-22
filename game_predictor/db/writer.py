import pytds

from .queries import UPSERT_GAME_ODDS, UPSERT_SPREAD_TOTAL


def save_predictions(conn: pytds.Connection, predictions: list[dict]):
    with conn.cursor() as cursor:
        for pred in predictions:
            cursor.execute(
                UPSERT_GAME_ODDS,
                (
                    pred["GameId"],
                    pred["ModelId"],
                    pred["RunDateUTC"],
                    pred["HomeOdds"],
                    pred["AwayOdds"],
                    pred["LogLoss"],
                    pred["Notes"],
                ),
            )
    conn.commit()


def save_spread_total_predictions(conn: pytds.Connection, predictions: list[dict]):
    with conn.cursor() as cursor:
        for pred in predictions:
            cursor.execute(
                UPSERT_SPREAD_TOTAL,
                (
                    pred["GameId"],
                    pred["ModelId"],
                    pred["RunDateUTC"],
                    pred["PredictedValue"],
                    pred["ResidualStd"],
                    pred.get("Line"),
                    pred.get("CoverProbability"),
                    pred["Notes"],
                ),
            )
    conn.commit()
