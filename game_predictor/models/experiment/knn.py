from sklearn.neighbors import KNeighborsClassifier

from .types import ModelConfig


def knn(
    n_neighbors: int = 5,
    weights: str = "uniform",
    metric: str = "minkowski",
    p: int = 2,
) -> ModelConfig:
    """Create a K-Nearest Neighbors model config."""
    return ModelConfig(
        cls=KNeighborsClassifier,
        params={
            "n_neighbors": n_neighbors,
            "weights": weights,
            "metric": metric,
            "p": p,
        },
    )


def tune_knn(X_train, X_test, y_train, y_test, n_trials, progress_callback, sample_weight=None):
    import optuna
    from sklearn.metrics import log_loss

    def objective(trial):
        params = {
            "n_neighbors": trial.suggest_int("n_neighbors", 3, 500),
            "weights": trial.suggest_categorical("weights", ["uniform", "distance"]),
            "metric": trial.suggest_categorical("metric", ["minkowski", "cosine"]),
            "p": trial.suggest_int("p", 1, 3),
        }
        model = KNeighborsClassifier(**params)
        model.fit(X_train, y_train)
        return log_loss(y_test, model.predict_proba(X_test))

    study = optuna.create_study(direction="minimize", study_name="knn-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
