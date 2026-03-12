import math
import time
import warnings
from datetime import datetime, timezone

from .config import HOMEGROWN_MODEL_ID, get_db_config
from .db.connection import get_connection
from .db.queries import FEATURE_COLUMNS
from .db.reader import load_training_data, load_unplayed_games
from .db.writer import save_predictions
from .models.training import train_all, train_default
from .prediction.experiments import SAVE_EXPERIMENT

warnings.filterwarnings("ignore", message="X does not have valid feature names")


def _calculate_log_loss(winner: int, home_odds: float, away_odds: float) -> float:
    if home_odds <= 0 or away_odds <= 0:
        return -1.0
    return -(winner * math.log(away_odds) + (1 - winner) * math.log(home_odds))


def _save_test_predictions(conn, save_pipeline, save_model, save_name, test_df, run_date):
    X_test_save = save_pipeline.transform(test_df[FEATURE_COLUMNS].values)
    test_proba = save_model.predict_proba(X_test_save)

    test_predictions = []
    for i, row in test_df.iterrows():
        idx = test_df.index.get_loc(i)
        home_odds = float(test_proba[idx][0])
        away_odds = float(test_proba[idx][1])
        game_log_loss = _calculate_log_loss(int(row["Winner"]), home_odds, away_odds)

        test_predictions.append(
            {
                "GameId": int(row["GameId"]),
                "ModelId": HOMEGROWN_MODEL_ID,
                "RunDateUTC": run_date,
                "HomeOdds": home_odds,
                "AwayOdds": away_odds,
                "LogLoss": game_log_loss,
                "Notes": f"{SAVE_EXPERIMENT}/{save_name}",
            }
        )

    print(f"\nSaving {len(test_predictions)} test set predictions (last 2 seasons) to GameOdds...")
    save_predictions(conn, test_predictions)


def _save_unplayed_predictions(conn, save_pipeline, save_model, save_name, run_date):
    unplayed_df = load_unplayed_games(conn)

    if unplayed_df.empty:
        print("\nNo unplayed games to predict.")
        return

    X_unplayed = save_pipeline.transform(unplayed_df[FEATURE_COLUMNS].values)
    unplayed_proba = save_model.predict_proba(X_unplayed)

    unplayed_predictions = []
    for i, row in unplayed_df.iterrows():
        idx = unplayed_df.index.get_loc(i)
        home_odds = float(unplayed_proba[idx][0])
        away_odds = float(unplayed_proba[idx][1])

        unplayed_predictions.append(
            {
                "GameId": int(row["GameId"]),
                "ModelId": HOMEGROWN_MODEL_ID,
                "RunDateUTC": run_date,
                "HomeOdds": home_odds,
                "AwayOdds": away_odds,
                "LogLoss": 0.0,
                "Notes": f"{SAVE_EXPERIMENT}/{save_name}",
            }
        )

    print(f"Saving {len(unplayed_predictions)} unplayed game predictions to GameOdds...")
    save_predictions(conn, unplayed_predictions)


def run(mode: str = "predict", shap: bool = False):
    start = time.time()
    config = get_db_config()
    conn = get_connection(config)

    print("Loading data...")
    train_df = load_training_data(conn)

    if mode == "backfill":
        result = train_all(train_df, shap=shap)
        if result is None:
            conn.close()
            return

        save_pipeline, save_model, save_name, test_df = result
        run_date = datetime.now(timezone.utc)

        _save_test_predictions(conn, save_pipeline, save_model, save_name, test_df, run_date)
    else:
        result = train_default(train_df)
        if result is None:
            conn.close()
            return

        save_pipeline, save_model, save_name = result
        run_date = datetime.now(timezone.utc)

    _save_unplayed_predictions(conn, save_pipeline, save_model, save_name, run_date)

    elapsed = time.time() - start
    minutes, seconds = divmod(int(elapsed), 60)
    print(f"Done in {minutes}m {seconds}s.")
    conn.close()
