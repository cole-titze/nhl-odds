import warnings
from datetime import datetime, timezone

from ..config import get_db_config
from ..db.connection import get_connection
from ..db.queries import FEATURE_COLUMNS
from ..db.reader import load_team_names, load_training_data, load_unplayed_games
from ..db.writer import save_predictions
from ..models.trainer import build_models, train_and_evaluate
from .predictor import EXPERIMENTS, SAVE_EXPERIMENT

warnings.filterwarnings("ignore", message="X does not have valid feature names")


def _final_result(results: dict) -> tuple[str, dict]:
    if "Ensemble" in results:
        return "Ensemble", results["Ensemble"]
    name = next(iter(results))
    return name, results[name]


def run():
    config = get_db_config()
    conn = get_connection(config)

    print("Loading data...")
    train_df = load_training_data(conn)
    unplayed_df = load_unplayed_games(conn)
    team_names = load_team_names(conn)

    if train_df.empty:
        print("No training data found.")
        conn.close()
        return

    current_season = train_df["SeasonStartYear"].max()
    train_mask = train_df["SeasonStartYear"] < current_season
    test_mask = train_df["SeasonStartYear"] == current_season

    X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
    y_train = train_df.loc[train_mask, "Winner"].values
    X_test_raw = train_df.loc[test_mask, FEATURE_COLUMNS].values
    y_test = train_df.loc[test_mask, "Winner"].values
    has_unplayed = not unplayed_df.empty
    X_future_raw = unplayed_df[FEATURE_COLUMNS].values if has_unplayed else None

    print(f"Training on seasons before {current_season} ({len(X_train_raw)} games)")
    print(f"Testing on season {current_season} ({len(X_test_raw)} games)")

    all_experiment_results = {}

    for exp_name, exp in EXPERIMENTS.items():
        print(f"\n{'=' * 60}")
        print(f"  Experiment: {exp_name}")
        print(f"{'=' * 60}")

        pipeline = exp["pipeline"]
        X_train_t = pipeline.fit_transform(X_train_raw, y_train)
        X_test_t = pipeline.transform(X_test_raw)

        models = build_models(exp["models"])
        results = train_and_evaluate(models, X_train_t, X_test_t, y_train, y_test, exp["ensemble"])

        print("\n  Model Evaluation:")
        for name, metrics in results.items():
            print(f"    {name:>10s}  accuracy={metrics['accuracy']:.4f}  log_loss={metrics['log_loss']:.4f}")

        all_experiment_results[exp_name] = {
            "results": results,
            "pipeline": pipeline,
        }

    # Summary comparison
    print(f"\n{'=' * 60}")
    print("  Summary")
    print(f"{'=' * 60}")
    print(f"  {'Experiment':<20} {'Model':>10}  {'Accuracy':>8}  {'LogLoss':>8}")
    print(f"  {'-' * 55}")
    for exp_name, exp_data in all_experiment_results.items():
        name, metrics = _final_result(exp_data["results"])
        print(f"  {exp_name:<20} {name:>10}  {metrics['accuracy']:>8.4f}  {metrics['log_loss']:>8.4f}")

    # Predictions and saving from the designated experiment
    if not has_unplayed:
        print("\nNo unplayed games to predict.")
        conn.close()
        return

    save_exp = all_experiment_results[SAVE_EXPERIMENT]
    save_pipeline = save_exp["pipeline"]
    X_future = save_pipeline.transform(X_future_raw)

    save_name, save_metrics = _final_result(save_exp["results"])
    save_model = save_metrics["model"]
    proba = save_model.predict_proba(X_future)

    print(f"\n--- Predictions using '{SAVE_EXPERIMENT}' / {save_name} ({len(unplayed_df)} games) ---")
    print(f"{'Date':<12} {'Home':>25} {'Away':>25}   {'Home%':>6} {'Away%':>6}")
    print("-" * 90)

    run_date = datetime.now(timezone.utc)
    all_predictions = []

    for i, row in unplayed_df.iterrows():
        home_name = team_names.get(row["HomeTeamId"], f"Team {row['HomeTeamId']}")
        away_name = team_names.get(row["AwayTeamId"], f"Team {row['AwayTeamId']}")
        game_date = (
            row["GameDateUTC"].strftime("%Y-%m-%d")
            if hasattr(row["GameDateUTC"], "strftime")
            else str(row["GameDateUTC"])[:10]
        )
        idx = unplayed_df.index.get_loc(i)
        home_pct = proba[idx][0] * 100
        away_pct = proba[idx][1] * 100
        print(f"{game_date:<12} {home_name:>25} {away_name:>25}   {home_pct:5.1f}% {away_pct:5.1f}%")

        all_predictions.append(
            {
                "GameId": int(row["GameId"]),
                "ModelName": 1,
                "RunDateUTC": run_date,
                "HomeOdds": float(proba[idx][0]),
                "AwayOdds": float(proba[idx][1]),
                "LogLoss": float(save_metrics["log_loss"]),
                "Notes": f"{SAVE_EXPERIMENT}/{save_name}",
            }
        )

    print(f"\nSaving {len(all_predictions)} predictions to GameOdds...")
    save_predictions(conn, all_predictions)
    print("Done.")

    conn.close()
