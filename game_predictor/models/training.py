import numpy as np
from sklearn.metrics import log_loss as sklearn_log_loss
from sklearn.pipeline import Pipeline

from ..db.queries import FEATURE_COLUMNS
from ..prediction.experiments import EXPERIMENTS, SAVE_EXPERIMENT
from .calibration import calibrate_model
from .ensemble import Ensemble
from .trainer import build_models, train_and_evaluate


def _final_result(results: dict) -> tuple[str, dict]:
    if "Ensemble" in results:
        return "Ensemble", results["Ensemble"]
    name = next(iter(results))
    return name, results[name]


def _run_tuning(exp_name, exp, X_train_t, X_test_t, y_train, y_test) -> dict:
    """Run Optuna tuning for each model in the experiment.

    Returns a dict of model_name -> tuned sklearn model instance.
    """
    from .tuner import PARAM_CONVERTERS, TUNERS, print_best_params, progress_callback

    tuned_models = {}
    for model_name, model_cfg in exp.models.items():
        cls_name = model_cfg.cls.__name__
        tune_fn = TUNERS.get(cls_name)
        if tune_fn is None:
            continue

        print(f"    Tuning {model_name} ({exp.tune_trials} trials)...")
        study = tune_fn(X_train_t, X_test_t, y_train, y_test, exp.tune_trials, progress_callback(exp.tune_trials))
        print_best_params(study, model_name)

        converter = PARAM_CONVERTERS.get(cls_name)
        if converter:
            tuned_params = converter(study)
        else:
            tuned_params = {**model_cfg.params, **study.best_params}
        tuned_models[model_name] = model_cfg.cls(**tuned_params)

    return tuned_models


def _run_experiment(exp_name, exp, X_train_raw, X_cal_raw, X_test_raw, y_train, y_cal, y_test):
    """Run a single experiment: pipeline tuning, model tuning, training, calibration."""
    from .experiment.pipeline import standard_pipeline, tune_pipeline
    from .tuner import print_best_params, progress_callback

    pipeline = exp.pipeline

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
        )
        print_best_params(pipe_study, "pipeline")
        pipeline = standard_pipeline(**pipe_study.best_params)

    X_train_t = pipeline.fit_transform(X_train_raw, y_train)
    X_cal_t = pipeline.transform(X_cal_raw)
    X_test_t = pipeline.transform(X_test_raw)

    if exp.tune:
        tuned_models = _run_tuning(exp_name, exp, X_train_t, X_cal_t, y_train, y_cal)
    else:
        tuned_models = {}

    models = build_models(exp.models)
    models.update(tuned_models)
    results = train_and_evaluate(models, X_train_t, X_test_t, y_train, y_test, exp.ensemble)

    name, metrics = _final_result(results)

    if exp.calibration:
        calibrated_model = calibrate_model(metrics["model"], X_cal_t, y_cal, X_test_t, y_test, method=exp.calibration)

        cal_proba = calibrated_model.predict_proba(X_test_t)
        cal_pred = np.argmax(cal_proba, axis=1)

        results[f"{name} (calibrated)"] = {
            "model": calibrated_model,
            "accuracy": float(np.mean(cal_pred == y_test)),
            "log_loss": sklearn_log_loss(y_test, cal_proba),
        }

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
    pipeline = exp.pipeline
    X_train_t = pipeline.fit_transform(X_train_raw, y_train)
    X_cal_t = pipeline.transform(X_cal_raw)

    built = build_models(exp.models)
    for model in built.values():
        model.fit(X_train_t, y_train)

    if exp.ensemble and len(exp.ensemble) > 1:
        ensemble_models = [built[n] for n in exp.ensemble]
        save_model = Ensemble(models=ensemble_models)
        save_model.fit(X_train_t, y_train)
        save_name = "Ensemble"
    else:
        save_name = next(iter(built))
        save_model = built[save_name]

    if exp.calibration:
        save_model = calibrate_model(save_model, X_cal_t, y_cal, X_cal_t, y_cal, method=exp.calibration)

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
