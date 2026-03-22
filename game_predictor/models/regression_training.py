import numpy as np
from sklearn.metrics import mean_absolute_error, mean_squared_error

from ..db.queries import FEATURE_COLUMNS
from ..prediction.experiments import REGRESSION_EXPERIMENTS, SAVE_SPREAD_EXPERIMENT, SAVE_TOTAL_EXPERIMENT
from .ensemble import RegressionEnsemble
from .trainer import _fit, build_models


def _compute_weights(seasons: np.ndarray, decay: float) -> np.ndarray | None:
    if decay == 0.0:
        return None
    seasons_ago = seasons.max() - seasons
    weights = np.exp(-decay * seasons_ago)
    return weights / weights.mean()


def _compute_target(df, target: str) -> np.ndarray:
    if target == "spread":
        return (df["HomeGoals"] - df["AwayGoals"]).values.astype(float)
    elif target == "total":
        return (df["HomeGoals"] + df["AwayGoals"]).values.astype(float)
    raise ValueError(f"Unknown target: {target}")


def _final_result(results: dict) -> tuple[str, dict]:
    if "Ensemble" in results:
        return "Ensemble", results["Ensemble"]
    name = next(iter(results))
    return name, results[name]


def _train_and_evaluate_regression(models, X_train, X_test, y_train, y_test, ensemble_names, sample_weight=None):
    results = {}

    for name, model in models.items():
        _fit(model, X_train, y_train, sample_weight)
        y_pred = model.predict(X_test)
        results[name] = {
            "model": model,
            "mae": mean_absolute_error(y_test, y_pred),
            "rmse": float(np.sqrt(mean_squared_error(y_test, y_pred))),
        }

    if ensemble_names and len(ensemble_names) > 1:
        ensemble_models = [results[n]["model"] for n in ensemble_names]
        ensemble_model = RegressionEnsemble(models=ensemble_models)
        ensemble_model.fit(X_train, y_train)
        y_pred = ensemble_model.predict(X_test)
        results["Ensemble"] = {
            "model": ensemble_model,
            "mae": mean_absolute_error(y_test, y_pred),
            "rmse": float(np.sqrt(mean_squared_error(y_test, y_pred))),
        }

    return results


def train_regression_default(train_df, target: str):
    """Train regression model for spread or total prediction.

    Holds out most recent season for evaluation and residual std computation.
    Returns (pipeline, model, name, residual_std) or None.
    """
    exp_name = SAVE_SPREAD_EXPERIMENT if target == "spread" else SAVE_TOTAL_EXPERIMENT
    if not exp_name or exp_name not in REGRESSION_EXPERIMENTS:
        return None

    exp = REGRESSION_EXPERIMENTS[exp_name]

    if train_df.empty:
        return None

    y_all = _compute_target(train_df, target)
    current_season = train_df["SeasonStartYear"].max()
    train_mask = train_df["SeasonStartYear"] < current_season
    cal_mask = train_df["SeasonStartYear"] == current_season

    X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
    y_train = y_all[train_mask.values]
    X_cal_raw = train_df.loc[cal_mask, FEATURE_COLUMNS].values
    y_cal = y_all[cal_mask.values]
    w_train = _compute_weights(train_df.loc[train_mask, "SeasonStartYear"].values, exp.decay)

    if len(X_train_raw) < 50:
        return None

    print(f"  Training '{exp_name}' ({target}) on {len(X_train_raw)} games, eval on {len(X_cal_raw)} games")

    pipeline = exp.pipeline
    X_train_t = pipeline.fit_transform(X_train_raw, y_train)

    built = build_models(exp.models)
    for model in built.values():
        _fit(model, X_train_t, y_train, w_train)

    if exp.ensemble and len(exp.ensemble) > 1:
        ensemble_models = [built[n] for n in exp.ensemble]
        save_model = RegressionEnsemble(models=ensemble_models)
        save_model.fit(X_train_t, y_train)
        save_name = "Ensemble"
    else:
        save_name = next(iter(built))
        save_model = built[save_name]

    # Compute residual std on held-out season
    if len(X_cal_raw) > 0:
        X_cal_t = pipeline.transform(X_cal_raw)
        residuals = y_cal - save_model.predict(X_cal_t)
        residual_std = float(np.std(residuals))
        mae = float(mean_absolute_error(y_cal, save_model.predict(X_cal_t)))
        print(f"  {target} eval: MAE={mae:.3f}, residual_std={residual_std:.3f}")
    else:
        residual_std = float(np.std(y_train))

    return pipeline, save_model, save_name, residual_std


def train_regression_for_day(train_df, target: str):
    """Train regression model for walk-forward backfill.

    Splits most recent season as eval set when possible.
    Returns (pipeline, model, name, residual_std) or None.
    """
    exp_name = SAVE_SPREAD_EXPERIMENT if target == "spread" else SAVE_TOTAL_EXPERIMENT
    if not exp_name or exp_name not in REGRESSION_EXPERIMENTS:
        return None

    exp = REGRESSION_EXPERIMENTS[exp_name]

    if train_df.empty or len(train_df) < 50:
        return None

    y_all = _compute_target(train_df, target)

    MIN_CAL_GAMES = 20
    current_season = train_df["SeasonStartYear"].max()
    train_mask = train_df["SeasonStartYear"] < current_season
    cal_mask = train_df["SeasonStartYear"] == current_season

    can_split = train_mask.sum() >= 50 and cal_mask.sum() >= MIN_CAL_GAMES

    if can_split:
        X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
        y_train = y_all[train_mask.values]
        w_train = _compute_weights(train_df.loc[train_mask, "SeasonStartYear"].values, exp.decay)
        X_cal_raw = train_df.loc[cal_mask, FEATURE_COLUMNS].values
        y_cal = y_all[cal_mask.values]
    else:
        X_train_raw = train_df[FEATURE_COLUMNS].values
        y_train = y_all
        w_train = _compute_weights(train_df["SeasonStartYear"].values, exp.decay)

    from .experiment.pipeline import standard_pipeline

    n_samples, n_features = X_train_raw.shape
    k_best = min(exp.pipeline.named_steps["select"].k, n_features)
    pca_components = min(exp.pipeline.named_steps["pca"].n_components, k_best, n_samples)
    pipeline = standard_pipeline(k_best=k_best, pca_components=pca_components)
    X_train_t = pipeline.fit_transform(X_train_raw, y_train)

    built = build_models(exp.models)
    for model in built.values():
        if hasattr(model, "n_neighbors") and model.n_neighbors > n_samples:
            model.n_neighbors = max(1, n_samples - 1)
        _fit(model, X_train_t, y_train, w_train)

    if exp.ensemble and len(exp.ensemble) > 1:
        ensemble_models = [built[n] for n in exp.ensemble]
        save_model = RegressionEnsemble(models=ensemble_models)
        save_model.fit(X_train_t, y_train)
        save_name = "Ensemble"
    else:
        save_name = next(iter(built))
        save_model = built[save_name]

    if can_split:
        X_cal_t = pipeline.transform(X_cal_raw)
        residuals = y_cal - save_model.predict(X_cal_t)
        residual_std = float(np.std(residuals))
    else:
        residuals = y_train - save_model.predict(X_train_t)
        residual_std = float(np.std(residuals))

    return pipeline, save_model, save_name, residual_std


def train_regression_all(train_df):
    """Train all regression experiments with train/test split. Used by test mode."""
    if train_df.empty:
        print("No training data for regression.")
        return

    TEST_SEASONS = 2
    current_season = train_df["SeasonStartYear"].max()
    test_start = current_season - TEST_SEASONS + 1
    train_mask = train_df["SeasonStartYear"] < test_start
    test_mask = train_df["SeasonStartYear"] >= test_start

    X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
    X_test_raw = train_df.loc[test_mask, FEATURE_COLUMNS].values
    train_seasons = train_df.loc[train_mask, "SeasonStartYear"].values

    print(f"\nRegression: training on {len(X_train_raw)} games, testing on {len(X_test_raw)} games")

    all_results = {}

    for exp_name, exp in REGRESSION_EXPERIMENTS.items():
        for target in [exp.target]:
            y_train = _compute_target(train_df.loc[train_mask], target)
            y_test = _compute_target(train_df.loc[test_mask], target)
            w_train = _compute_weights(train_seasons, exp.decay)

            pipeline = exp.pipeline
            X_train_t = pipeline.fit_transform(X_train_raw, y_train)
            X_test_t = pipeline.transform(X_test_raw)

            built = build_models(exp.models)
            results = _train_and_evaluate_regression(
                built, X_train_t, X_test_t, y_train, y_test, exp.ensemble, sample_weight=w_train
            )
            all_results[exp_name] = results

    print(f"\n{'=' * 60}")
    print("  Regression Summary")
    print(f"{'=' * 60}")
    print(f"  {'Experiment':<20} {'Model':>22}  {'MAE':>8}  {'RMSE':>8}")
    print(f"  {'-' * 55}")
    for exp_name, results in all_results.items():
        for model_name, metrics in results.items():
            print(f"  {exp_name:<20} {model_name:>22}  {metrics['mae']:>8.4f}  {metrics['rmse']:>8.4f}")
