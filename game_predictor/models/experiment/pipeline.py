from sklearn.decomposition import PCA
from sklearn.feature_selection import SelectKBest, f_classif
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

from ...db.queries import FEATURE_COLUMNS

N_FEATURES = len(FEATURE_COLUMNS)


def standard_pipeline(k_best: int = 50, pca_components: int = 15) -> Pipeline:
    """Standard preprocessing pipeline used by most experiments."""
    return Pipeline(
        [
            ("scaler", StandardScaler()),
            ("select", SelectKBest(f_classif, k=k_best)),
            ("pca", PCA(n_components=pca_components)),
        ]
    )


def tune_pipeline(X_train, X_test, y_train, y_test, model_cls, model_params, n_trials, progress_callback):
    """Tune k_best and pca_components using a given model as the evaluator."""
    import optuna
    from sklearn.metrics import log_loss

    def objective(trial):
        k_best = trial.suggest_int("k_best", 10, N_FEATURES)
        pca_components = trial.suggest_int("pca_components", 5, k_best)

        pipe = standard_pipeline(k_best=k_best, pca_components=pca_components)
        X_train_t = pipe.fit_transform(X_train, y_train)
        X_test_t = pipe.transform(X_test)

        model = model_cls(**model_params)
        model.fit(X_train_t, y_train)
        return log_loss(y_test, model.predict_proba(X_test_t))

    study = optuna.create_study(direction="minimize", study_name="pipeline-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
