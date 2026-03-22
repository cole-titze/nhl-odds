import math
import time
import warnings
from datetime import datetime, timezone

import pandas as pd

from .config import HOMEGROWN_MODEL_ID, get_db_config
from .db.connection import get_connection
from .db.queries import FEATURE_COLUMNS
from .db.reader import load_training_data, load_unplayed_games
from .db.writer import save_predictions
from .models.training import train_all, train_default, train_for_day
from .prediction.experiments import SAVE_EXPERIMENT

warnings.filterwarnings("ignore", message="X does not have valid feature names")


def _calculate_log_loss(winner: int, home_odds: float, away_odds: float) -> float:
    if home_odds <= 0 or away_odds <= 0:
        return -1.0
    return -(winner * math.log(away_odds) + (1 - winner) * math.log(home_odds))


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


def _run_backfill(conn, train_df):
    """Walk-forward backfill: for each game day, train on all prior games and predict that day."""
    train_df = train_df.sort_values("GameDateUTC").reset_index(drop=True)
    train_df["GameDate"] = pd.to_datetime(train_df["GameDateUTC"]).dt.date

    SAVE_INTERVAL = 50

    game_days = sorted(train_df["GameDate"].unique())
    run_date = datetime.now(timezone.utc)
    batch_predictions = []
    total_saved = 0
    total_correct = 0
    total_log_loss = 0.0

    print(f"Walk-forward backfill: {len(game_days)} game days, {len(train_df)} total games\n")

    for day_idx, day in enumerate(game_days):
        day_mask = train_df["GameDate"] == day
        before_mask = train_df["GameDate"] < day

        day_games = train_df.loc[day_mask]
        prior_games = train_df.loc[before_mask]

        result = train_for_day(prior_games)
        if result is None:
            continue

        pipeline, model, name = result

        X_day = pipeline.transform(day_games[FEATURE_COLUMNS].values)
        proba = model.predict_proba(X_day)

        for i, (_, row) in enumerate(day_games.iterrows()):
            home_odds = float(proba[i][0])
            away_odds = float(proba[i][1])
            winner = int(row["Winner"])
            game_log_loss = _calculate_log_loss(winner, home_odds, away_odds)

            predicted_winner = 0 if home_odds >= 0.5 else 1
            if predicted_winner == winner:
                total_correct += 1
            total_log_loss += game_log_loss

            batch_predictions.append(
                {
                    "GameId": int(row["GameId"]),
                    "ModelId": HOMEGROWN_MODEL_ID,
                    "RunDateUTC": run_date,
                    "HomeOdds": home_odds,
                    "AwayOdds": away_odds,
                    "LogLoss": game_log_loss,
                    "Notes": f"{SAVE_EXPERIMENT}/{name}",
                }
            )

        if (day_idx + 1) % SAVE_INTERVAL == 0 or day_idx == len(game_days) - 1:
            n = total_saved + len(batch_predictions)
            avg_ll = total_log_loss / n if n > 0 else 0
            acc = total_correct / n if n > 0 else 0
            print(
                f"  Day {day_idx + 1}/{len(game_days)} ({day}): {n} games, log loss: {avg_ll:.4f}, accuracy: {acc:.4f}"
            )
            if batch_predictions:
                save_predictions(conn, batch_predictions)
                total_saved += len(batch_predictions)
                batch_predictions = []

    if total_saved > 0:
        avg_ll = total_log_loss / total_saved
        acc = total_correct / total_saved
        print(f"\nBackfill complete: {total_saved} games predicted")
        print(f"  Accuracy: {acc:.4f}")
        print(f"  Log Loss: {avg_ll:.4f}")


def run(mode: str = "predict"):
    start = time.time()
    config = get_db_config()
    conn = get_connection(config)

    print("Loading data...")
    train_df = load_training_data(conn)

    if mode == "backfill":
        _run_backfill(conn, train_df)
    elif mode == "test":
        train_all(train_df)
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
