import numpy as np
from sklearn.metrics import mean_absolute_error, mean_squared_error

from ..db.queries import FEATURE_COLUMNS
from ..prediction.experiments import REGRESSION_EXPERIMENTS, SAVE_SPREAD_EXPERIMENT, SAVE_TOTAL_EXPERIMENT
from .ensemble import RegressionEnsemble
from .trainer import _fit, build_models

_REGRESSOR_TO_FACTORY = {
    "LGBMRegressor": "lgbm_regressor",
    "XGBRegressor": "xgboost_regressor",
    "MLPRegressor": "mlp_regressor",
    "RandomForestRegressor": "random_forest_regressor",
}

_INTERNAL_PARAMS = {"random_state", "verbosity", "early_stopping"}


def _fmt(v) -> str:
    if isinstance(v, float):
        return f"{v:.6g}"
    if isinstance(v, str):
        return f'"{v}"'
    return repr(v)


def _print_tuned_regression_config(exp_name, exp, pipeline_params, tuned_decay, tuned_model_params):
    indent = "    "
    lines = [f'\n  # --- tuned config for "{exp_name}" ---']
    lines.append(f'  "{exp_name}": RegressionExperiment(')
    lines.append(f"{indent}models={{")
    for model_name, (cls_name, best_params) in tuned_model_params.items():
        factory = _REGRESSOR_TO_FACTORY.get(cls_name, cls_name)
        args = ", ".join(f"{k}={_fmt(v)}" for k, v in best_params.items() if k not in _INTERNAL_PARAMS)
        lines.append(f'{indent}    "{model_name}": {factory}({args}),')
    lines.append(f"{indent}}},")

    k_best = pipeline_params.get("k_best", 50)
    pca = pipeline_params.get("pca_components", 15)
    lines.append(f"{indent}pipeline=standard_pipeline(k_best={k_best}, pca_components={pca}, regression=True),")

    if exp.ensemble:
        lines.append(f"{indent}ensemble={exp.ensemble!r},")

    lines.append(f'{indent}target="{exp.target}",')
    lines.append(f"{indent}decay={tuned_decay:.4g},")
    lines.append(f"{indent}tune=False,")
    lines.append("  ),")

    print("\n".join(lines))


def _run_regression_tuning(exp_name, exp, X_train_t, X_test_t, y_train, y_test, sample_weight=None):
    """Run Optuna tuning for each regression model.

    Returns (tuned_models, tuned_params) where tuned_params is
    {model_name: (cls_name, params_dict)} for copy-paste config generation.
    """
    from .tuner import REGRESSION_PARAM_CONVERTERS, REGRESSION_TUNERS, print_best_params, progress_callback

    tuned_models = {}
    tuned_params = {}
    for model_name, model_cfg in exp.models.items():
        cls_name = model_cfg.cls.__name__
        tune_fn = REGRESSION_TUNERS.get(cls_name)
        if tune_fn is None:
            continue

        print(f"    Tuning {model_name} ({exp.tune_trials} trials)...")
        study = tune_fn(
            X_train_t,
            X_test_t,
            y_train,
            y_test,
            exp.tune_trials,
            progress_callback(exp.tune_trials, metric_name="MAE"),
            sample_weight=sample_weight,
        )
        print_best_params(study, model_name, metric_name="MAE")

        converter = REGRESSION_PARAM_CONVERTERS.get(cls_name)
        if converter:
            params = converter(study)
        else:
            params = {**model_cfg.params, **study.best_params}
        tuned_models[model_name] = model_cfg.cls(**params)
        tuned_params[model_name] = (cls_name, study.best_params)

    return tuned_models, tuned_params


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


def train_regression_for_season(train_df, target: str):
    """Train regression model on all prior seasons' data (no residual std yet).

    Called once per season during backfill. Residual std is computed per-day
    via compute_residual_std().
    Returns (pipeline, model, name) or None.
    """
    exp_name = SAVE_SPREAD_EXPERIMENT if target == "spread" else SAVE_TOTAL_EXPERIMENT
    if not exp_name or exp_name not in REGRESSION_EXPERIMENTS:
        return None

    exp = REGRESSION_EXPERIMENTS[exp_name]

    if train_df.empty or len(train_df) < 50:
        return None

    y = _compute_target(train_df, target)
    X_raw = train_df[FEATURE_COLUMNS].values
    w = _compute_weights(train_df["SeasonStartYear"].values, exp.decay)

    from .experiment.pipeline import standard_pipeline

    n_samples, n_features = X_raw.shape
    k_best = min(exp.pipeline.named_steps["select"].k, n_features)
    pca_components = min(exp.pipeline.named_steps["pca"].n_components, k_best, n_samples)
    pipeline = standard_pipeline(k_best=k_best, pca_components=pca_components, regression=True)
    X_t = pipeline.fit_transform(X_raw, y)

    built = build_models(exp.models)
    for model in built.values():
        if hasattr(model, "n_neighbors") and model.n_neighbors > n_samples:
            model.n_neighbors = max(1, n_samples - 1)
        _fit(model, X_t, y, w)

    if exp.ensemble and len(exp.ensemble) > 1:
        ensemble_models = [built[n] for n in exp.ensemble]
        save_model = RegressionEnsemble(models=ensemble_models)
        save_model.fit(X_t, y)
        save_name = "Ensemble"
    else:
        save_name = next(iter(built))
        save_model = built[save_name]

    return pipeline, save_model, save_name


MIN_RESIDUAL_GAMES = 20


def compute_residual_std(model, pipeline, cal_df, target: str) -> float:
    """Compute residual std from a pre-trained model using season games so far."""
    if len(cal_df) < MIN_RESIDUAL_GAMES:
        return 2.5  # reasonable default for NHL goal diffs / totals

    y_cal = _compute_target(cal_df, target)
    X_cal = pipeline.transform(cal_df[FEATURE_COLUMNS].values)
    residuals = y_cal - model.predict(X_cal)
    return float(np.std(residuals))


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
    pipeline = standard_pipeline(k_best=k_best, pca_components=pca_components, regression=True)
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
    cal_season = test_start - 1
    train_mask = train_df["SeasonStartYear"] < cal_season
    cal_mask = train_df["SeasonStartYear"] == cal_season
    test_mask = train_df["SeasonStartYear"] >= test_start

    X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
    X_cal_raw = train_df.loc[cal_mask, FEATURE_COLUMNS].values
    X_test_raw = train_df.loc[test_mask, FEATURE_COLUMNS].values
    train_seasons = train_df.loc[train_mask, "SeasonStartYear"].values

    print(f"\nRegression: training on {len(X_train_raw)} games, testing on {len(X_test_raw)} games")

    all_results = {}

    for exp_name, exp in REGRESSION_EXPERIMENTS.items():
        print(f"\n  {exp_name}...")
        y_train = _compute_target(train_df.loc[train_mask], exp.target)
        y_cal = _compute_target(train_df.loc[cal_mask], exp.target)
        y_test = _compute_target(train_df.loc[test_mask], exp.target)
        w_train = _compute_weights(train_seasons, exp.decay)

        pipeline = exp.pipeline
        pipeline_best_params: dict = {}
        tuned_decay = exp.decay

        if exp.tune:
            from .experiment.pipeline import tune_regression_pipeline
            from .tuner import print_best_params, progress_callback

            first_cfg = next(iter(exp.models.values()))
            print(f"    Tuning pipeline ({exp.tune_trials} trials)...")
            pipe_study = tune_regression_pipeline(
                X_train_raw,
                X_cal_raw,
                y_train,
                y_cal,
                first_cfg.cls,
                first_cfg.params,
                exp.tune_trials,
                progress_callback(exp.tune_trials, metric_name="MAE"),
                train_seasons=train_seasons,
            )
            print_best_params(pipe_study, "pipeline", metric_name="MAE")
            pipeline_best_params = pipe_study.best_params.copy()
            tuned_decay = pipeline_best_params.pop("decay", exp.decay)
            from .experiment.pipeline import standard_pipeline

            pipeline = standard_pipeline(**pipeline_best_params, regression=True)
            w_train = _compute_weights(train_seasons, tuned_decay)

        X_train_t = pipeline.fit_transform(X_train_raw, y_train)
        X_cal_t = pipeline.transform(X_cal_raw)
        X_test_t = pipeline.transform(X_test_raw)

        tuned_model_params = {}
        if exp.tune:
            tuned_models, tuned_model_params = _run_regression_tuning(
                exp_name, exp, X_train_t, X_cal_t, y_train, y_cal, sample_weight=w_train
            )
        else:
            tuned_models = {}

        built = build_models(exp.models)
        built.update(tuned_models)
        results = _train_and_evaluate_regression(
            built, X_train_t, X_test_t, y_train, y_test, exp.ensemble, sample_weight=w_train
        )
        all_results[exp_name] = results

        if exp.tune:
            _print_tuned_regression_config(exp_name, exp, pipeline_best_params, tuned_decay, tuned_model_params)

    print(f"\n{'=' * 60}")
    print("  Regression Summary")
    print(f"{'=' * 60}")
    print(f"  {'Experiment':<20} {'Model':>22}  {'MAE':>8}  {'RMSE':>8}")
    print(f"  {'-' * 55}")
    for exp_name, results in all_results.items():
        for model_name, metrics in results.items():
            print(f"  {exp_name:<20} {model_name:>22}  {metrics['mae']:>8.4f}  {metrics['rmse']:>8.4f}")
