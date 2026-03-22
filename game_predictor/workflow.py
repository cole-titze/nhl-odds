import math
import time
import warnings
from datetime import datetime, timezone

import pandas as pd

from .config import HOMEGROWN_MODEL_ID, SPREAD_MODEL_ID, TOTAL_MODEL_ID, get_db_config
from .db.connection import get_connection
from .db.queries import FEATURE_COLUMNS
from .db.reader import load_consensus_lines, load_training_data, load_unplayed_games
from .db.writer import save_predictions, save_spread_total_predictions
from .models.probability import cover_probability
from .models.regression_training import (
    compute_residual_std,
    train_regression_all,
    train_regression_default,
    train_regression_for_season,
)
from .models.training import calibrate_for_day, train_all, train_default, train_for_season
from .prediction.experiments import (
    SAVE_EXPERIMENT,
    SAVE_SPREAD_EXPERIMENT,
    SAVE_TOTAL_EXPERIMENT,
)

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
    """Walk-forward backfill: train once per season, recalibrate per day."""
    train_df = train_df.sort_values("GameDateUTC").reset_index(drop=True)
    train_df["GameDate"] = pd.to_datetime(train_df["GameDateUTC"]).dt.date

    SAVE_INTERVAL = 50
    seasons = sorted(train_df["SeasonStartYear"].unique())
    run_date = datetime.now(timezone.utc)
    batch_predictions = []
    total_saved = 0
    total_correct = 0
    total_log_loss = 0.0
    day_count = 0

    total_days = train_df["GameDate"].nunique()
    print(f"Walk-forward backfill: {len(seasons)} seasons, {total_days} game days, {len(train_df)} total games\n")

    for season in seasons:
        prior_data = train_df[train_df["SeasonStartYear"] < season]
        season_data = train_df[train_df["SeasonStartYear"] == season]

        result = train_for_season(prior_data)
        if result is None:
            day_count += season_data["GameDate"].nunique()
            continue

        pipeline, base_model, name = result
        season_days = sorted(season_data["GameDate"].unique())
        print(f"  Season {season}: trained on {len(prior_data)} games, predicting {len(season_data)} games")

        for day in season_days:
            day_count += 1
            day_games = season_data[season_data["GameDate"] == day]
            cal_games = season_data[season_data["GameDate"] < day]

            cal_model = calibrate_for_day(base_model, pipeline, cal_games)

            X_day = pipeline.transform(day_games[FEATURE_COLUMNS].values)
            proba = cal_model.predict_proba(X_day)

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

            if day_count % SAVE_INTERVAL == 0 or (season == seasons[-1] and day == season_days[-1]):
                n = total_saved + len(batch_predictions)
                avg_ll = total_log_loss / n if n > 0 else 0
                acc = total_correct / n if n > 0 else 0
                print(
                    f"    Day {day_count}/{total_days} ({day}): {n} games, log loss: {avg_ll:.4f}, accuracy: {acc:.4f}"
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


def _save_unplayed_regression(conn, pipeline, model, residual_std, target, model_id, exp_name, run_date):
    """Predict spread or total for unplayed games and save to DB."""
    unplayed_df = load_unplayed_games(conn)
    if unplayed_df.empty:
        print(f"\nNo unplayed games for {target} prediction.")
        return

    consensus = load_consensus_lines(conn)
    X = pipeline.transform(unplayed_df[FEATURE_COLUMNS].values)
    preds = model.predict(X)

    predictions = []
    for i, (_, row) in enumerate(unplayed_df.iterrows()):
        game_id = int(row["GameId"])
        predicted = float(preds[i])
        line_key = "spread" if target == "spread" else "total"
        game_lines = consensus.get(game_id, {})
        line = game_lines.get(line_key)
        cover_prob = cover_probability(predicted, residual_std, line) if line is not None else None

        predictions.append(
            {
                "GameId": game_id,
                "ModelId": model_id,
                "RunDateUTC": run_date,
                "PredictedValue": predicted,
                "ResidualStd": residual_std,
                "Line": line,
                "CoverProbability": cover_prob,
                "Notes": f"{exp_name}/Ensemble",
            }
        )

    print(f"Saving {len(predictions)} {target} predictions...")
    save_spread_total_predictions(conn, predictions)


def _run_regression_backfill(conn, train_df, target, model_id, exp_name):
    """Walk-forward backfill for regression: train once per season, update residual std per day."""
    train_df = train_df.sort_values("GameDateUTC").reset_index(drop=True)
    train_df["GameDate"] = pd.to_datetime(train_df["GameDateUTC"]).dt.date

    SAVE_INTERVAL = 50
    seasons = sorted(train_df["SeasonStartYear"].unique())
    run_date = datetime.now(timezone.utc)
    consensus = load_consensus_lines(conn)
    batch = []
    total_saved = 0
    total_ae = 0.0
    day_count = 0

    total_days = train_df["GameDate"].nunique()
    print(f"\n{target.title()} backfill: {len(seasons)} seasons, {total_days} game days")

    for season in seasons:
        prior_data = train_df[train_df["SeasonStartYear"] < season]
        season_data = train_df[train_df["SeasonStartYear"] == season]

        result = train_regression_for_season(prior_data, target)
        if result is None:
            day_count += season_data["GameDate"].nunique()
            continue

        pipeline, base_model, name = result
        season_days = sorted(season_data["GameDate"].unique())

        for day in season_days:
            day_count += 1
            day_games = season_data[season_data["GameDate"] == day]
            cal_games = season_data[season_data["GameDate"] < day]

            residual_std = compute_residual_std(base_model, pipeline, cal_games, target)

            X_day = pipeline.transform(day_games[FEATURE_COLUMNS].values)
            preds = base_model.predict(X_day)

            if target == "spread":
                actuals = (day_games["HomeGoals"] - day_games["AwayGoals"]).values.astype(float)
            else:
                actuals = (day_games["HomeGoals"] + day_games["AwayGoals"]).values.astype(float)

            for i, (_, row) in enumerate(day_games.iterrows()):
                game_id = int(row["GameId"])
                predicted = float(preds[i])
                actual = float(actuals[i])
                total_ae += abs(predicted - actual)

                line_key = "spread" if target == "spread" else "total"
                game_lines = consensus.get(game_id, {})
                line = game_lines.get(line_key)
                cover_prob = cover_probability(predicted, residual_std, line) if line is not None else None

                batch.append(
                    {
                        "GameId": game_id,
                        "ModelId": model_id,
                        "RunDateUTC": run_date,
                        "PredictedValue": predicted,
                        "ResidualStd": residual_std,
                        "Line": line,
                        "CoverProbability": cover_prob,
                        "Notes": f"{exp_name}/{name}",
                    }
                )

            if day_count % SAVE_INTERVAL == 0 or (season == seasons[-1] and day == season_days[-1]):
                n = total_saved + len(batch)
                mae = total_ae / n if n > 0 else 0
                print(f"  Day {day_count}/{total_days} ({day}): {n} games, MAE: {mae:.3f}")
                if batch:
                    save_spread_total_predictions(conn, batch)
                    total_saved += len(batch)
                    batch = []

    if total_saved > 0:
        print(f"\n{target.title()} backfill complete: {total_saved} games, MAE: {total_ae / total_saved:.3f}")


def run(mode: str = "predict"):
    start = time.time()
    config = get_db_config()
    conn = get_connection(config)

    print("Loading data...")
    train_df = load_training_data(conn)

    if mode == "backfill":
        _run_backfill(conn, train_df)
        if SAVE_SPREAD_EXPERIMENT:
            _run_regression_backfill(conn, train_df, "spread", SPREAD_MODEL_ID, SAVE_SPREAD_EXPERIMENT)
        if SAVE_TOTAL_EXPERIMENT:
            _run_regression_backfill(conn, train_df, "total", TOTAL_MODEL_ID, SAVE_TOTAL_EXPERIMENT)
    elif mode == "test":
        train_all(train_df)
        train_regression_all(train_df)
    else:
        result = train_default(train_df)
        if result is None:
            conn.close()
            return

        save_pipeline, save_model, save_name = result
        run_date = datetime.now(timezone.utc)

        _save_unplayed_predictions(conn, save_pipeline, save_model, save_name, run_date)

        for target, model_id, exp_name in [
            ("spread", SPREAD_MODEL_ID, SAVE_SPREAD_EXPERIMENT),
            ("total", TOTAL_MODEL_ID, SAVE_TOTAL_EXPERIMENT),
        ]:
            if not exp_name:
                continue
            reg_result = train_regression_default(train_df, target)
            if reg_result is None:
                continue
            reg_pipeline, reg_model, reg_name, residual_std = reg_result
            _save_unplayed_regression(conn, reg_pipeline, reg_model, residual_std, target, model_id, exp_name, run_date)

    elapsed = time.time() - start
    minutes, seconds = divmod(int(elapsed), 60)
    print(f"Done in {minutes}m {seconds}s.")
    conn.close()
