import numpy as np

from .queries import UPSERT_GAME_ODDS, UPSERT_PREDICTION_FACTOR, UPSERT_SPREAD_TOTAL


def _native(value):
    """Convert numpy scalars to Python native types for psycopg2."""
    if isinstance(value, np.integer):
        return int(value)
    if isinstance(value, np.floating):
        return float(value)
    return value


def save_predictions(conn, predictions: list[dict]):
    with conn.cursor() as cursor:
        for pred in predictions:
            cursor.execute(
                UPSERT_GAME_ODDS,
                (
                    _native(pred["GameId"]),
                    _native(pred["ModelId"]),
                    pred["RunDateUTC"],
                    _native(pred["HomeOdds"]),
                    _native(pred["AwayOdds"]),
                    _native(pred["LogLoss"]),
                    pred["Notes"],
                ),
            )
    conn.commit()


def save_spread_total_predictions(conn, predictions: list[dict]):
    with conn.cursor() as cursor:
        for pred in predictions:
            cursor.execute(
                UPSERT_SPREAD_TOTAL,
                (
                    _native(pred["GameId"]),
                    _native(pred["ModelId"]),
                    pred["RunDateUTC"],
                    _native(pred["PredictedValue"]),
                    _native(pred["ResidualStd"]),
                    _native(pred.get("Line")),
                    _native(pred.get("CoverProbability")),
                    pred["Notes"],
                ),
            )
    conn.commit()


def save_prediction_factors(conn, factors: list[dict], model_id: int, run_date):
    with conn.cursor() as cursor:
        cursor.executemany(
            UPSERT_PREDICTION_FACTOR,
            [
                (
                    _native(f["GameId"]),
                    model_id,
                    run_date,
                    _native(f["Rank"]),
                    f["Factor"],
                    f["Detail"],
                    _native(f["Contribution"]),
                    _native(f["BaseHomeOdds"]),
                )
                for f in factors
            ],
        )
    conn.commit()
