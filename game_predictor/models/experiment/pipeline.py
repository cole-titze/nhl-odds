from sklearn.decomposition import PCA
from sklearn.feature_selection import SelectKBest, f_classif, f_regression
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

from ...db.queries import FEATURE_COLUMNS

N_FEATURES = len(FEATURE_COLUMNS)


def standard_pipeline(k_best: int = 50, pca_components: int | None = 15, regression: bool = False) -> Pipeline:
    """Standard preprocessing pipeline used by most experiments. pca_components=None skips PCA."""
    score_func = f_regression if regression else f_classif
    steps = [
        ("scaler", StandardScaler()),
        ("select", SelectKBest(score_func, k=k_best)),
    ]
    if pca_components is not None:
        steps.append(("pca", PCA(n_components=pca_components)))
    return Pipeline(steps)


def _tune_pipeline(raw_folds, model_cls, model_params, n_trials, progress_callback, regression, study_name):
    """Tune k_best, pca_components and decay, scoring each trial across all folds.

    raw_folds: list of (X_train_raw, X_val_raw, y_train, y_val, train_seasons). The pipeline is
    refit on each fold's training seasons so selection/PCA never see validation data.
    """
    import numpy as np
    import optuna

    from .folds import log_loss_score, mae_score, mean_fold_score

    def make_model():
        model = model_cls(**model_params)
        # One thread per trial — Optuna already runs trials in parallel
        if "n_jobs" in model.get_params():
            model.set_params(n_jobs=1)
        elif hasattr(model, "get_all_params"):  # CatBoost names it thread_count
            model.set_params(thread_count=1)
        return model

    def objective(trial):
        k_best = trial.suggest_int("k_best", 10, N_FEATURES)
        pca_components = trial.suggest_int("pca_components", 5, k_best)
        decay = trial.suggest_float("decay", 0.0, 1.0)

        folds = []
        for X_train, X_val, y_train, y_val, train_seasons in raw_folds:
            sample_weight = None
            if decay > 0.0:
                w = np.exp(-decay * (train_seasons.max() - train_seasons))
                sample_weight = w / w.mean()
            pipe = standard_pipeline(k_best=k_best, pca_components=pca_components, regression=regression)
            folds.append((pipe.fit_transform(X_train, y_train), pipe.transform(X_val), y_train, y_val, sample_weight))

        return mean_fold_score(make_model, folds, mae_score if regression else log_loss_score)

    study = optuna.create_study(direction="minimize", study_name=study_name)
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study


def tune_pipeline(raw_folds, model_cls, model_params, n_trials, progress_callback):
    return _tune_pipeline(raw_folds, model_cls, model_params, n_trials, progress_callback, False, "pipeline-tuning")


def tune_regression_pipeline(raw_folds, model_cls, model_params, n_trials, progress_callback):
    return _tune_pipeline(
        raw_folds, model_cls, model_params, n_trials, progress_callback, True, "regression-pipeline-tuning"
    )
