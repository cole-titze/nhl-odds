import numpy as np
from sklearn.ensemble import StackingClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import log_loss as sklearn_log_loss
from sklearn.pipeline import Pipeline

from ..db.queries import FEATURE_COLUMNS
from ..prediction.experiments import EXPERIMENTS, SAVE_EXPERIMENT
from .calibration import calibrate_model
from .ensemble import Ensemble
from .trainer import _fit, build_models, train_and_evaluate


def _compute_weights(seasons: np.ndarray, decay: float) -> np.ndarray | None:
    if decay == 0.0:
        return None
    seasons_ago = seasons.max() - seasons
    weights = np.exp(-decay * seasons_ago)
    return weights / weights.mean()


_CLS_TO_FACTORY = {
    "LGBMClassifier": "lgbm",
    "XGBClassifier": "xgboost",
    "MLPClassifier": "mlp",
    "RandomForestClassifier": "random_forest",
    "KNeighborsClassifier": "knn",
    "LogisticRegression": "logistic_regression",
}

# Params set internally by factories — omit from copy-paste output
_INTERNAL_PARAMS = {"random_state", "verbosity", "use_label_encoder", "early_stopping", "solver", "max_iter"}


def _fmt(v) -> str:
    if isinstance(v, float):
        return f"{v:.6g}"
    if isinstance(v, str):
        return f'"{v}"'
    return repr(v)


def _print_tuned_config(exp_name, exp, pipeline_params, tuned_decay, tuned_model_params, cal_method):
    from sklearn.calibration import CalibratedClassifierCV

    used_cal = cal_method.method if isinstance(cal_method, CalibratedClassifierCV) else "none"

    indent = "    "
    lines = [f'\n  # --- tuned config for "{exp_name}" ---']
    lines.append(f'  "{exp_name}": Experiment(')
    lines.append(f"{indent}models={{")
    for model_name, (cls_name, best_params) in tuned_model_params.items():
        factory = _CLS_TO_FACTORY.get(cls_name, cls_name)
        args = ", ".join(f"{k}={_fmt(v)}" for k, v in best_params.items() if k not in _INTERNAL_PARAMS)
        lines.append(f'{indent}    "{model_name}": {factory}({args}),')
    lines.append(f"{indent}}},")

    k_best = pipeline_params.get("k_best", 50)
    pca = pipeline_params.get("pca_components", 15)
    lines.append(f"{indent}pipeline=standard_pipeline(k_best={k_best}, pca_components={pca}),")

    if exp.ensemble:
        lines.append(f"{indent}ensemble={exp.ensemble!r},")
    if exp.stack:
        lines.append(f"{indent}stack=True,")

    lines.append(f'{indent}calibration="{used_cal}",')
    lines.append(f"{indent}decay={tuned_decay:.4g},")
    lines.append(f"{indent}tune=False,")
    lines.append("  ),")

    print("\n".join(lines))


def _final_result(results: dict) -> tuple[str, dict]:
    if "Ensemble" in results:
        return "Ensemble", results["Ensemble"]
    name = next(iter(results))
    return name, results[name]


def _run_tuning(exp_name, exp, X_train_t, X_test_t, y_train, y_test, sample_weight=None) -> tuple[dict, dict]:
    """Run Optuna tuning for each model in the experiment.

    Returns (tuned_models, tuned_params) where tuned_params is
    {model_name: (cls_name, params_dict)} for copy-paste config generation.
    """
    from .tuner import PARAM_CONVERTERS, TUNERS, print_best_params, progress_callback

    tuned_models = {}
    tuned_params = {}
    for model_name, model_cfg in exp.models.items():
        cls_name = model_cfg.cls.__name__
        tune_fn = TUNERS.get(cls_name)
        if tune_fn is None:
            continue

        print(f"    Tuning {model_name} ({exp.tune_trials} trials)...")
        study = tune_fn(
            X_train_t,
            X_test_t,
            y_train,
            y_test,
            exp.tune_trials,
            progress_callback(exp.tune_trials),
            sample_weight=sample_weight,
        )
        print_best_params(study, model_name)

        converter = PARAM_CONVERTERS.get(cls_name)
        if converter:
            params = converter(study)
        else:
            params = {**model_cfg.params, **study.best_params}
        tuned_models[model_name] = model_cfg.cls(**params)
        tuned_params[model_name] = (cls_name, study.best_params)

    return tuned_models, tuned_params


def _run_experiment(exp_name, exp, X_train_raw, X_cal_raw, X_test_raw, y_train, y_cal, y_test, train_seasons=None):
    """Run a single experiment: pipeline tuning, model tuning, training, calibration."""
    from .experiment.pipeline import standard_pipeline, tune_pipeline
    from .tuner import print_best_params, progress_callback

    pipeline = exp.pipeline
    sample_weight = _compute_weights(train_seasons, exp.decay) if train_seasons is not None else None
    tuned_decay = exp.decay
    pipeline_best_params: dict = {}

    # Tune pipeline params using the first model as evaluator
    if exp.tune:
        first_cfg = next(iter(exp.models.values()))
        print(f"    Tuning pipeline ({exp.tune_trials} trials)...")
        pipe_study = tune_pipeline(
            X_train_raw,
            X_cal_raw,
            y_train,
            y_cal,
            first_cfg.cls,
            first_cfg.params,
            exp.tune_trials,
            progress_callback(exp.tune_trials),
            train_seasons=train_seasons,
        )
        print_best_params(pipe_study, "pipeline")
        pipeline_best_params = pipe_study.best_params.copy()
        tuned_decay = pipeline_best_params.pop("decay", exp.decay)
        pipeline = standard_pipeline(**pipeline_best_params)
        sample_weight = _compute_weights(train_seasons, tuned_decay) if train_seasons is not None else None

    X_train_t = pipeline.fit_transform(X_train_raw, y_train)
    X_cal_t = pipeline.transform(X_cal_raw)
    X_test_t = pipeline.transform(X_test_raw)

    tuned_model_params = {}
    if exp.tune:
        tuned_models, tuned_model_params = _run_tuning(
            exp_name, exp, X_train_t, X_cal_t, y_train, y_cal, sample_weight=sample_weight
        )
    else:
        tuned_models = {}

    models = build_models(exp.models)
    models.update(tuned_models)
    results = train_and_evaluate(
        models, X_train_t, X_test_t, y_train, y_test, exp.ensemble, stack=exp.stack, sample_weight=sample_weight
    )

    name, metrics = _final_result(results)

    final_model = metrics["model"]
    cal_method = ["sigmoid", "isotonic"] if exp.tune else exp.calibration
    if cal_method and cal_method != "none":
        calibrated_model = calibrate_model(metrics["model"], X_cal_t, y_cal, X_test_t, y_test, method=cal_method)
        final_model = calibrated_model

        cal_proba = calibrated_model.predict_proba(X_test_t)
        cal_pred = np.argmax(cal_proba, axis=1)

        results[f"{name} (calibrated)"] = {
            "model": calibrated_model,
            "accuracy": float(np.mean(cal_pred == y_test)),
            "log_loss": sklearn_log_loss(y_test, cal_proba),
        }

    if exp.tune:
        _print_tuned_config(exp_name, exp, pipeline_best_params, tuned_decay, tuned_model_params, final_model)

    return exp_name, results, pipeline


def train_default(train_df):
    """Train only the default experiment. Holds out most recent season for calibration.

    Used by predict mode (Docker).
    Returns (pipeline, model, model_name) or None.
    """
    if train_df.empty:
        print("No training data found.")
        return None

    current_season = train_df["SeasonStartYear"].max()
    train_mask = train_df["SeasonStartYear"] < current_season
    cal_mask = train_df["SeasonStartYear"] == current_season

    X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
    y_train = train_df.loc[train_mask, "Winner"].values
    X_cal_raw = train_df.loc[cal_mask, FEATURE_COLUMNS].values
    y_cal = train_df.loc[cal_mask, "Winner"].values

    print(
        f"Training '{SAVE_EXPERIMENT}' on {len(X_train_raw)} games,"
        f" calibrating on {len(X_cal_raw)} games (season {current_season})"
    )

    exp = EXPERIMENTS[SAVE_EXPERIMENT]
    w_train = _compute_weights(train_df.loc[train_mask, "SeasonStartYear"].values, exp.decay)
    w_cal = _compute_weights(train_df.loc[cal_mask, "SeasonStartYear"].values, exp.decay)

    pipeline = exp.pipeline
    X_train_t = pipeline.fit_transform(X_train_raw, y_train)
    X_cal_t = pipeline.transform(X_cal_raw)

    built = build_models(exp.models)
    for model in built.values():
        _fit(model, X_train_t, y_train, w_train)

    if exp.ensemble and len(exp.ensemble) > 1:
        if exp.stack:
            from sklearn.base import clone

            estimators = [(n, clone(built[n])) for n in exp.ensemble]
            save_model = StackingClassifier(
                estimators=estimators,
                final_estimator=LogisticRegression(),
                cv=5,
                stack_method="predict_proba",
                n_jobs=-1,
            )
            save_model.fit(X_train_t, y_train)
        else:
            ensemble_models = [built[n] for n in exp.ensemble]
            save_model = Ensemble(models=ensemble_models)
            save_model.fit(X_train_t, y_train)
        save_name = "Ensemble"
    else:
        save_name = next(iter(built))
        save_model = built[save_name]

    if exp.calibration:
        save_model = calibrate_model(
            save_model, X_cal_t, y_cal, X_cal_t, y_cal, method=exp.calibration, sample_weight=w_cal
        )

    return pipeline, save_model, save_name


def train_all(train_df, shap: bool = False):
    """Train all experiments in parallel with train/calibration/test split.

    Used by backfill mode (IDE experimenting).
    Returns (pipeline, model, model_name, test_df) or None.
    """
    if train_df.empty:
        print("No training data found.")
        return None

    TEST_SEASONS = 2
    current_season = train_df["SeasonStartYear"].max()
    test_start_season = current_season - TEST_SEASONS + 1

    cal_season = test_start_season - 1
    train_mask = train_df["SeasonStartYear"] < cal_season
    cal_mask = train_df["SeasonStartYear"] == cal_season
    test_mask = train_df["SeasonStartYear"] >= test_start_season

    X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
    y_train = train_df.loc[train_mask, "Winner"].values
    X_cal_raw = train_df.loc[cal_mask, FEATURE_COLUMNS].values
    y_cal = train_df.loc[cal_mask, "Winner"].values
    X_test_raw = train_df.loc[test_mask, FEATURE_COLUMNS].values
    y_test = train_df.loc[test_mask, "Winner"].values

    print(f"Training on seasons before {cal_season} ({len(X_train_raw)} games)")
    print(f"Calibrating on season {cal_season} ({len(X_cal_raw)} games)")
    print(f"Testing on seasons {test_start_season}-{current_season} ({len(X_test_raw)} games)")

    train_seasons = train_df.loc[train_mask, "SeasonStartYear"].values

    if shap:
        from .analysis import run_shap_analysis

        run_shap_analysis(X_train_raw, X_test_raw, y_train, y_test, FEATURE_COLUMNS)

    all_experiment_results = {}

    for exp_name, exp in EXPERIMENTS.items():
        print(f"\n  {exp_name}...")
        exp_name, results, pipeline = _run_experiment(
            exp_name,
            exp,
            X_train_raw,
            X_cal_raw,
            X_test_raw,
            y_train,
            y_cal,
            y_test,
            train_seasons=train_seasons,
        )
        all_experiment_results[exp_name] = {
            "results": results,
            "pipeline": pipeline,
        }

    # Summary (in experiment definition order)
    print(f"\n{'=' * 70}")
    print("  Summary")
    print(f"{'=' * 70}")
    print(f"  {'Experiment':<20} {'Model':>22}  {'Accuracy':>8}  {'LogLoss':>8}")
    print(f"  {'-' * 65}")
    for exp_name in EXPERIMENTS:
        exp_data = all_experiment_results[exp_name]
        for model_name, metrics in exp_data["results"].items():
            print(f"  {exp_name:<20} {model_name:>22}  {metrics['accuracy']:>8.4f}  {metrics['log_loss']:>8.4f}")

    # Use the calibrated version for saving (if it exists), otherwise uncalibrated
    save_exp = all_experiment_results[SAVE_EXPERIMENT]
    save_pipeline: Pipeline = save_exp["pipeline"]
    save_name_raw, save_metrics = _final_result(save_exp["results"])
    cal_key = f"{save_name_raw} (calibrated)"
    if cal_key in save_exp["results"]:
        save_model = save_exp["results"][cal_key]["model"]
        save_name = cal_key
    else:
        save_model = save_metrics["model"]
        save_name = save_name_raw

    test_df = train_df.loc[test_mask]
    return save_pipeline, save_model, save_name, test_df
