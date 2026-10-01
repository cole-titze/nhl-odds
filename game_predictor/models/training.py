from dataclasses import dataclass

import numpy as np
import pandas as pd
from sklearn.base import clone
from sklearn.calibration import CalibratedClassifierCV
from sklearn.frozen import FrozenEstimator
from sklearn.metrics import log_loss as sklearn_log_loss
from sklearn.pipeline import Pipeline

from ..db.queries import FEATURE_COLUMNS
from ..prediction.experiments import EXPERIMENTS, SAVE_EXPERIMENT
from .calibration import calibrate_model
from .ensemble import Ensemble, WeightedStackingClassifier
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
_INTERNAL_PARAMS = {"random_state", "verbosity", "use_label_encoder", "early_stopping", "solver"}


def _fmt(v) -> str:
    if isinstance(v, float):
        return f"{v:.6g}"
    if isinstance(v, str):
        return f'"{v}"'
    return repr(v)


def _print_tuned_config(exp_name, exp, pipeline_params, tuned_decay, tuned_model_params, calibration):
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

    lines.append(f'{indent}calibration="{calibration}",')
    lines.append(f"{indent}decay={tuned_decay:.4g},")
    lines.append(f"{indent}tune=False,")
    lines.append("  ),")

    print("\n".join(lines))


def _final_result(results: dict) -> tuple[str, dict]:
    if "Ensemble" in results:
        return "Ensemble", results["Ensemble"]
    name = next(iter(results))
    return name, results[name]


def train_default(train_df):
    """Train only the default experiment.

    With calibration, holds out the most recent season to calibrate on; with calibration
    "none", trains on every season (holding one out would just discard it).

    Used by predict mode (Docker).
    Returns (pipeline, model, model_name) or None.
    """
    if train_df.empty:
        print("No training data found.")
        return None

    exp = EXPERIMENTS[SAVE_EXPERIMENT]
    calibrate = bool(exp.calibration) and exp.calibration != "none"

    current_season = train_df["SeasonStartYear"].max()
    if calibrate:
        train_mask = train_df["SeasonStartYear"] < current_season
    else:
        train_mask = pd.Series(True, index=train_df.index)
    cal_mask = train_df["SeasonStartYear"] == current_season

    X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
    y_train = train_df.loc[train_mask, "Winner"].values
    X_cal_raw = train_df.loc[cal_mask, FEATURE_COLUMNS].values
    y_cal = train_df.loc[cal_mask, "Winner"].values

    if calibrate:
        print(
            f"Training '{SAVE_EXPERIMENT}' on {len(X_train_raw)} games,"
            f" calibrating on {len(X_cal_raw)} games (season {current_season})"
        )
    else:
        print(f"Training '{SAVE_EXPERIMENT}' on {len(X_train_raw)} games (all seasons, no calibration)")

    w_train = _compute_weights(train_df.loc[train_mask, "SeasonStartYear"].values, exp.decay)
    w_cal = _compute_weights(train_df.loc[cal_mask, "SeasonStartYear"].values, exp.decay)

    pipeline = exp.pipeline
    X_train_t = pipeline.fit_transform(X_train_raw, y_train)

    built = build_models(exp.models)
    for model in built.values():
        _fit(model, X_train_t, y_train, w_train)

    if exp.ensemble and len(exp.ensemble) > 1:
        if exp.stack:
            from sklearn.base import clone

            estimators = [(n, clone(built[n])) for n in exp.ensemble]
            save_model = WeightedStackingClassifier(estimators=estimators)
            save_model.fit(X_train_t, y_train, sample_weight=w_train)
        else:
            ensemble_models = [built[n] for n in exp.ensemble]
            save_model = Ensemble(models=ensemble_models)
            save_model.fit(X_train_t, y_train)
        save_name = "Ensemble"
    else:
        save_name = next(iter(built))
        save_model = built[save_name]

    if calibrate:
        X_cal_t = pipeline.transform(X_cal_raw)
        save_model = calibrate_model(
            save_model, X_cal_t, y_cal, X_cal_t, y_cal, method=exp.calibration, sample_weight=w_cal
        )

    return pipeline, save_model, save_name


def train_for_season(train_df):
    """Train the save experiment on all prior seasons' data (no calibration).

    Used by walk-forward backfill — called once per season. Calibration
    happens separately per-day via calibrate_for_day().
    Returns (pipeline, model, name) or None.
    """
    if train_df.empty or len(train_df) < 50:
        return None

    exp = EXPERIMENTS[SAVE_EXPERIMENT]

    X_raw = train_df[FEATURE_COLUMNS].values
    y = train_df["Winner"].values
    w = _compute_weights(train_df["SeasonStartYear"].values, exp.decay)

    from .experiment.pipeline import standard_pipeline

    n_samples, n_features = X_raw.shape
    k_best = min(exp.pipeline.named_steps["select"].k, n_features)
    pca_components = min(exp.pipeline.named_steps["pca"].n_components, k_best, n_samples)
    pipeline = standard_pipeline(k_best=k_best, pca_components=pca_components)
    X_t = pipeline.fit_transform(X_raw, y)

    built = build_models(exp.models)
    for model in built.values():
        if hasattr(model, "n_neighbors") and model.n_neighbors > n_samples:
            model.n_neighbors = max(1, n_samples - 1)
        _fit(model, X_t, y, w)

    if exp.ensemble and len(exp.ensemble) > 1:
        if exp.stack:
            from sklearn.base import clone

            estimators = [(n, clone(built[n])) for n in exp.ensemble]
            save_model = WeightedStackingClassifier(estimators=estimators)
            save_model.fit(X_t, y, sample_weight=w)
        else:
            ensemble_models = [built[n] for n in exp.ensemble]
            save_model = Ensemble(models=ensemble_models)
            save_model.fit(X_t, y)
        save_name = "Ensemble"
    else:
        save_name = next(iter(built))
        save_model = built[save_name]

    return pipeline, save_model, save_name


MIN_CAL_GAMES = 20


def calibrate_for_day(model, pipeline, cal_df):
    """Recalibrate a pre-trained model using the season's games played so far.

    Returns the calibrated model, or the original if too few calibration games
    or calibration is disabled.
    """
    exp = EXPERIMENTS[SAVE_EXPERIMENT]
    if not exp.calibration or exp.calibration == "none":
        return model
    if len(cal_df) < MIN_CAL_GAMES:
        return model

    X_cal = pipeline.transform(cal_df[FEATURE_COLUMNS].values)
    y_cal = cal_df["Winner"].values
    w_cal = _compute_weights(cal_df["SeasonStartYear"].values, exp.decay)

    return calibrate_model(model, X_cal, y_cal, X_cal, y_cal, method=exp.calibration, sample_weight=w_cal)


def train_for_day(train_df):
    """Train the save experiment on provided data with calibration split.

    Used by walk-forward backfill. Splits most recent season as calibration
    set (matching train_default behavior). Skips calibration when there
    aren't enough seasons or calibration samples.
    Returns (pipeline, model, model_name) or None.
    """
    if train_df.empty or len(train_df) < 50:
        return None

    exp = EXPERIMENTS[SAVE_EXPERIMENT]

    # Split into train / cal using the most recent season
    MIN_CAL_GAMES = 20
    current_season = train_df["SeasonStartYear"].max()
    train_mask = train_df["SeasonStartYear"] < current_season
    cal_mask = train_df["SeasonStartYear"] == current_season

    can_calibrate = (
        exp.calibration and exp.calibration != "none" and train_mask.sum() >= 50 and cal_mask.sum() >= MIN_CAL_GAMES
    )

    if can_calibrate:
        X_train_raw = train_df.loc[train_mask, FEATURE_COLUMNS].values
        y_train = train_df.loc[train_mask, "Winner"].values
        w_train = _compute_weights(train_df.loc[train_mask, "SeasonStartYear"].values, exp.decay)
        X_cal_raw = train_df.loc[cal_mask, FEATURE_COLUMNS].values
        y_cal = train_df.loc[cal_mask, "Winner"].values
        w_cal = _compute_weights(train_df.loc[cal_mask, "SeasonStartYear"].values, exp.decay)
    else:
        X_train_raw = train_df[FEATURE_COLUMNS].values
        y_train = train_df["Winner"].values
        w_train = _compute_weights(train_df["SeasonStartYear"].values, exp.decay)

    # Cap pipeline params to fit available data size
    from .experiment.pipeline import standard_pipeline

    n_samples, n_features = X_train_raw.shape
    k_best = min(exp.pipeline.named_steps["select"].k, n_features)
    pca_components = min(exp.pipeline.named_steps["pca"].n_components, k_best, n_samples)
    pipeline = standard_pipeline(k_best=k_best, pca_components=pca_components)
    X_train_t = pipeline.fit_transform(X_train_raw, y_train)

    built = build_models(exp.models)
    for model in built.values():
        # Cap KNN neighbors to training size
        if hasattr(model, "n_neighbors") and model.n_neighbors > n_samples:
            model.n_neighbors = max(1, n_samples - 1)
        _fit(model, X_train_t, y_train, w_train)

    if exp.ensemble and len(exp.ensemble) > 1:
        if exp.stack:
            from sklearn.base import clone

            estimators = [(n, clone(built[n])) for n in exp.ensemble]
            save_model = WeightedStackingClassifier(estimators=estimators)
            save_model.fit(X_train_t, y_train, sample_weight=w_train)
        else:
            ensemble_models = [built[n] for n in exp.ensemble]
            save_model = Ensemble(models=ensemble_models)
            save_model.fit(X_train_t, y_train)
        save_name = "Ensemble"
    else:
        save_name = next(iter(built))
        save_model = built[save_name]

    if can_calibrate:
        X_cal_t = pipeline.transform(X_cal_raw)
        save_model = calibrate_model(
            save_model, X_cal_t, y_cal, X_cal_t, y_cal, method=exp.calibration, sample_weight=w_cal
        )

    return pipeline, save_model, save_name


# Test mode evaluates walk-forward: each scored season is predicted by a model trained only on
# earlier seasons, the same way production predicts an upcoming season.
EVAL_SEASONS = 4  # most recent seasons scored by test mode
TUNE_SEASONS = 4  # seasons just before the eval window that Optuna tuning scores trials on

_CALIBRATION_METHODS = ["none", "sigmoid", "isotonic"]


@dataclass
class _Split:
    """One walk-forward step: fit on `train`, calibrate on `cal` (if any), score `test`."""

    label: str
    train: pd.DataFrame
    cal: pd.DataFrame | None
    test: pd.DataFrame


def _walk_forward_splits(df: pd.DataFrame, seasons, calibrate: bool) -> list[_Split]:
    """One split per season S, scoring S with a model trained on every season before it.

    With calibration the base model trains on seasons before S-1 and is calibrated on S-1,
    mirroring train_default (train on prior seasons, calibrate on the latest one).
    """
    splits = []
    for season in seasons:
        test = df[df["SeasonStartYear"] == season]
        if calibrate:
            train = df[df["SeasonStartYear"] < season - 1]
            cal = df[df["SeasonStartYear"] == season - 1]
        else:
            train = df[df["SeasonStartYear"] < season]
            cal = None
        splits.append(_Split(str(season), train, cal, test))
    return splits


def _xy(df: pd.DataFrame):
    return df[FEATURE_COLUMNS].values, df["Winner"].values


def _fit_split(exp, split: _Split, pipeline_template: Pipeline, decay: float, models: dict):
    """Fit a fresh pipeline + models on split.train and score split.test.

    Returns (results, pipeline) where results is train_and_evaluate's per-model dict.
    """
    X_train, y_train = _xy(split.train)
    X_test, y_test = _xy(split.test)
    weights = _compute_weights(split.train["SeasonStartYear"].values, decay)

    pipeline = clone(pipeline_template)
    X_train_t = pipeline.fit_transform(X_train, y_train)
    X_test_t = pipeline.transform(X_test)

    fresh = {name: clone(model) for name, model in models.items()}
    results = train_and_evaluate(
        fresh, X_train_t, X_test_t, y_train, y_test, exp.ensemble, stack=exp.stack, sample_weight=weights
    )
    return results, pipeline


def _choose_calibration(exp, df, tune_seasons, pipeline_template, decay, models) -> str:
    """Pick none/sigmoid/isotonic by pooled log loss over the tuning seasons — never the eval seasons."""
    totals = dict.fromkeys(_CALIBRATION_METHODS, 0.0)
    for split in _walk_forward_splits(df, tune_seasons, calibrate=True):
        results, pipeline = _fit_split(exp, split, pipeline_template, decay, models)
        _, metrics = _final_result(results)
        X_cal, y_cal = _xy(split.cal)
        X_test, y_test = _xy(split.test)
        X_cal_t, X_test_t = pipeline.transform(X_cal), pipeline.transform(X_test)

        for method in _CALIBRATION_METHODS:
            model = metrics["model"]
            if method != "none":
                model = CalibratedClassifierCV(FrozenEstimator(model), method=method).fit(X_cal_t, y_cal)
            totals[method] += sklearn_log_loss(y_test, model.predict_proba(X_test_t), labels=[0, 1]) * len(y_test)

    return min(totals, key=totals.get)


def _tune_experiment(exp, df, tune_seasons):
    """Optuna-tune pipeline, decay, each model and calibration, scoring every trial across tune_seasons.

    Returns (pipeline_params, decay, models, tuned_model_params, calibration).
    """
    from .experiment.pipeline import standard_pipeline, tune_pipeline
    from .tuner import PARAM_CONVERTERS, TUNERS, print_best_params, progress_callback

    raw_folds = []
    for split in _walk_forward_splits(df, tune_seasons, calibrate=False):
        X_train, y_train = _xy(split.train)
        X_val, y_val = _xy(split.test)
        raw_folds.append((X_train, X_val, y_train, y_val, split.train["SeasonStartYear"].values))

    first_cfg = next(iter(exp.models.values()))
    print(f"    Tuning pipeline ({exp.tune_trials} trials)...")
    pipe_study = tune_pipeline(
        raw_folds, first_cfg.cls, first_cfg.params, exp.tune_trials, progress_callback(exp.tune_trials)
    )
    print_best_params(pipe_study, "pipeline")
    pipeline_params = pipe_study.best_params.copy()
    decay = pipeline_params.pop("decay")

    folds = []
    for X_train, X_val, y_train, y_val, train_seasons in raw_folds:
        pipeline = standard_pipeline(**pipeline_params)
        folds.append(
            (
                pipeline.fit_transform(X_train, y_train),
                pipeline.transform(X_val),
                y_train,
                y_val,
                _compute_weights(train_seasons, decay),
            )
        )

    models = build_models(exp.models)
    tuned_model_params = {}
    for model_name, model_cfg in exp.models.items():
        cls_name = model_cfg.cls.__name__
        tune_fn = TUNERS.get(cls_name)
        if tune_fn is None:
            continue

        print(f"    Tuning {model_name} ({exp.tune_trials} trials)...")
        study = tune_fn(folds, exp.tune_trials, progress_callback(exp.tune_trials))
        print_best_params(study, model_name)

        converter = PARAM_CONVERTERS.get(cls_name)
        params = converter(study) if converter else {**model_cfg.params, **study.best_params}
        models[model_name] = model_cfg.cls(**params)
        tuned_model_params[model_name] = (cls_name, params if converter else study.best_params)

    print("    Choosing calibration...")
    calibration = _choose_calibration(exp, df, tune_seasons, standard_pipeline(**pipeline_params), decay, models)
    print(f"    Calibration: {calibration}")
    return pipeline_params, decay, models, tuned_model_params, calibration


def _run_experiment(exp_name, exp, df, eval_seasons, tune_seasons, splits=None):
    """Tune (if enabled) then score the experiment on each walk-forward split.

    Returns a list of (split, {model_name: test probabilities}).
    """
    from .experiment.pipeline import standard_pipeline

    if exp.tune:
        pipeline_params, decay, models, tuned_model_params, calibration = _tune_experiment(exp, df, tune_seasons)
        pipeline_template = standard_pipeline(**pipeline_params)
    else:
        pipeline_template, decay, models = exp.pipeline, exp.decay, build_models(exp.models)
        calibration = exp.calibration or "none"

    if splits is None:
        splits = _walk_forward_splits(df, eval_seasons, calibrate=calibration != "none")

    outcomes = []
    for split in splits:
        results, pipeline = _fit_split(exp, split, pipeline_template, decay, models)
        probas = {name: metrics["proba"] for name, metrics in results.items()}

        if split.cal is not None and calibration != "none":
            name, metrics = _final_result(results)
            X_cal, y_cal = _xy(split.cal)
            X_cal_t = pipeline.transform(X_cal)
            # Same as train_default: calibrate on the held-out season, keep it only if it helps there
            cal_model = calibrate_model(metrics["model"], X_cal_t, y_cal, X_cal_t, y_cal, method=calibration)
            probas[f"{name} (calibrated)"] = cal_model.predict_proba(pipeline.transform(_xy(split.test)[0]))
        outcomes.append((split, probas))

    if exp.tune:
        _print_tuned_config(exp_name, exp, pipeline_params, decay, tuned_model_params, calibration)

    return outcomes


def _game_log_loss(y: np.ndarray, proba: np.ndarray) -> np.ndarray:
    eps = np.finfo(proba.dtype).eps
    return -np.log(np.clip(proba[np.arange(len(y)), y.astype(int)], eps, 1.0))


def _final_model_name(model_names) -> str:
    """The row production would use: calibrated if present, else the ensemble, else the single model."""
    for name in model_names:
        if name.endswith("(calibrated)"):
            return name
    return "Ensemble" if "Ensemble" in model_names else next(iter(model_names))


def _print_summary(all_outcomes: dict, eval_labels: list[str]):
    """Per-season and pooled scores for every model, then each experiment's final model vs the baseline."""
    season_cols = "".join(f"{label:>8}" for label in eval_labels)
    print(f"\n{'=' * 100}")
    print("  Summary — log loss by season (walk-forward), pooled log loss and accuracy")
    print(f"{'=' * 100}")
    print(f"  {'Experiment':<20} {'Model':>22}{season_cols}  {'LogLoss':>8}  {'Accuracy':>8}")
    print(f"  {'-' * 96}")

    final = {}  # exp_name -> (per-game losses, per-season log loss)
    for exp_name, outcomes in all_outcomes.items():
        model_names = list(outcomes[0][1])
        y_all = np.concatenate([_xy(split.test)[1] for split, _ in outcomes])
        for model_name in model_names:
            seasons = [sklearn_log_loss(_xy(s.test)[1], p[model_name], labels=[0, 1]) for s, p in outcomes]
            proba = np.concatenate([p[model_name] for _, p in outcomes])
            pooled = sklearn_log_loss(y_all, proba, labels=[0, 1])
            accuracy = float(np.mean(np.argmax(proba, axis=1) == y_all))
            cols = "".join(f"{ll:>8.4f}" for ll in seasons)
            print(f"  {exp_name:<20} {model_name:>22}{cols}  {pooled:>8.4f}  {accuracy:>8.4f}")
            if model_name == _final_model_name(model_names):
                final[exp_name] = (_game_log_loss(y_all, proba), seasons)

    if SAVE_EXPERIMENT not in final:
        return
    base_losses, base_seasons = final[SAVE_EXPERIMENT]
    print(f"\n  Final model vs '{SAVE_EXPERIMENT}' (paired per game; negative = better than baseline)")
    print(f"  {'Experiment':<20} {'ΔLogLoss':>9}  {'±SE':>7}  {'Seasons better':>15}")
    print(f"  {'-' * 56}")
    for exp_name, (losses, seasons) in final.items():
        if exp_name == SAVE_EXPERIMENT:
            continue
        diff = losses - base_losses
        se = diff.std(ddof=1) / np.sqrt(len(diff))
        better = sum(s < b for s, b in zip(seasons, base_seasons))
        flag = "  *" if abs(diff.mean()) > 2 * se else ""
        print(f"  {exp_name:<20} {diff.mean():>+9.4f}  {se:>7.4f}  {better:>9}/{len(seasons)}{flag}")
    print("  * = difference larger than 2 SE (unlikely to be noise; SE is approximate since games share teams/dates)")


def train_all(train_df):
    """Walk-forward evaluation of every experiment. Used by test mode (IDE experimenting).

    Scores each of the last EVAL_SEASONS seasons with models trained only on earlier seasons.
    Tuned experiments are tuned on the TUNE_SEASONS seasons before that window, so the eval
    seasons never influence tuning.
    """
    if train_df.empty:
        print("No training data found.")
        return

    last = int(train_df["SeasonStartYear"].max())
    eval_seasons = list(range(last - EVAL_SEASONS + 1, last + 1))
    tune_seasons = list(range(eval_seasons[0] - TUNE_SEASONS, eval_seasons[0]))
    n_eval = int(train_df["SeasonStartYear"].isin(eval_seasons).sum())

    print(f"Walk-forward evaluation on seasons {eval_seasons[0]}-{last} ({n_eval} games)")
    print(f"Tuning (tune=True experiments) on seasons {tune_seasons[0]}-{tune_seasons[-1]}")

    all_outcomes = {}
    for exp_name, exp in EXPERIMENTS.items():
        print(f"\n  {exp_name}...")
        all_outcomes[exp_name] = _run_experiment(exp_name, exp, train_df, eval_seasons, tune_seasons)

    _print_summary(all_outcomes, [str(s) for s in eval_seasons])
