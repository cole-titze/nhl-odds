import pytds

from .queries import UPSERT_GAME_ODDS


def save_predictions(conn: pytds.Connection, predictions: list[dict]):
    with conn.cursor() as cursor:
        for pred in predictions:
            cursor.execute(
                UPSERT_GAME_ODDS,
                (
                    pred["GameId"],
                    pred["ModelName"],
                    pred["RunDateUTC"],
                    pred["HomeOdds"],
                    pred["AwayOdds"],
                    pred["LogLoss"],
                    pred["Notes"],
                ),
            )
    conn.commit()
