from sklearn.decomposition import PCA
from sklearn.feature_selection import SelectKBest, f_classif, f_regression
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

from ...db.queries import FEATURE_COLUMNS

N_FEATURES = len(FEATURE_COLUMNS)


def standard_pipeline(k_best: int = 50, pca_components: int = 15, regression: bool = False) -> Pipeline:
    """Standard preprocessing pipeline used by most experiments."""
    score_func = f_regression if regression else f_classif
    return Pipeline(
        [
            ("scaler", StandardScaler()),
            ("select", SelectKBest(score_func, k=k_best)),
            ("pca", PCA(n_components=pca_components)),
        ]
    )


def tune_pipeline(
    X_train, X_test, y_train, y_test, model_cls, model_params, n_trials, progress_callback, train_seasons=None
):
    """Tune k_best, pca_components, and (if train_seasons provided) decay."""
    import inspect

    import numpy as np
    import optuna
    from sklearn.metrics import log_loss

    def objective(trial):
        k_best = trial.suggest_int("k_best", 10, N_FEATURES)
        pca_components = trial.suggest_int("pca_components", 5, k_best)

        sample_weight = None
        if train_seasons is not None:
            decay = trial.suggest_float("decay", 0.0, 1.0)
            if decay > 0.0:
                seasons_ago = train_seasons.max() - train_seasons
                w = np.exp(-decay * seasons_ago)
                sample_weight = w / w.mean()

        pipe = standard_pipeline(k_best=k_best, pca_components=pca_components)
        X_train_t = pipe.fit_transform(X_train, y_train)
        X_test_t = pipe.transform(X_test)

        model = model_cls(**model_params)
        if sample_weight is not None and "sample_weight" in inspect.signature(model.fit).parameters:
            model.fit(X_train_t, y_train, sample_weight=sample_weight)
        else:
            model.fit(X_train_t, y_train)
        return log_loss(y_test, model.predict_proba(X_test_t))

    study = optuna.create_study(direction="minimize", study_name="pipeline-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study


def tune_regression_pipeline(
    X_train, X_test, y_train, y_test, model_cls, model_params, n_trials, progress_callback, train_seasons=None
):
    """Tune k_best, pca_components, and (if train_seasons provided) decay for regression."""
    import inspect

    import numpy as np
    import optuna
    from sklearn.metrics import mean_absolute_error

    def objective(trial):
        k_best = trial.suggest_int("k_best", 10, N_FEATURES)
        pca_components = trial.suggest_int("pca_components", 5, k_best)

        sample_weight = None
        if train_seasons is not None:
            decay = trial.suggest_float("decay", 0.0, 1.0)
            if decay > 0.0:
                seasons_ago = train_seasons.max() - train_seasons
                w = np.exp(-decay * seasons_ago)
                sample_weight = w / w.mean()

        pipe = standard_pipeline(k_best=k_best, pca_components=pca_components, regression=True)
        X_train_t = pipe.fit_transform(X_train, y_train)
        X_test_t = pipe.transform(X_test)

        model = model_cls(**model_params)
        if sample_weight is not None and "sample_weight" in inspect.signature(model.fit).parameters:
            model.fit(X_train_t, y_train, sample_weight=sample_weight)
        else:
            model.fit(X_train_t, y_train)
        return mean_absolute_error(y_test, model.predict(X_test_t))

    study = optuna.create_study(direction="minimize", study_name="regression-pipeline-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
