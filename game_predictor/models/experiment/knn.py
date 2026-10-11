from sklearn.neighbors import KNeighborsClassifier, KNeighborsRegressor

from .folds import log_loss_score, mae_score, mean_fold_score
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


def tune_knn(folds, n_trials, progress_callback):
    import optuna

    def objective(trial):
        params = {
            "n_neighbors": trial.suggest_int("n_neighbors", 3, 500),
            "weights": trial.suggest_categorical("weights", ["uniform", "distance"]),
            "metric": trial.suggest_categorical("metric", ["minkowski", "cosine"]),
            "p": trial.suggest_int("p", 1, 3),
        }
        return mean_fold_score(lambda: KNeighborsClassifier(**params), folds, log_loss_score, use_weights=False)

    study = optuna.create_study(direction="minimize", study_name="knn-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study


def knn_regressor(
    n_neighbors: int = 5,
    weights: str = "uniform",
    metric: str = "minkowski",
    p: int = 2,
) -> ModelConfig:
    """Create a K-Nearest Neighbors regressor config (fit has no sample_weight, so decay is ignored)."""
    return ModelConfig(
        cls=KNeighborsRegressor,
        params={
            "n_neighbors": n_neighbors,
            "weights": weights,
            "metric": metric,
            "p": p,
        },
    )


def tune_knn_regressor(folds, n_trials, progress_callback):
    import optuna

    def objective(trial):
        params = {
            "n_neighbors": trial.suggest_int("n_neighbors", 3, 500),
            "weights": trial.suggest_categorical("weights", ["uniform", "distance"]),
            "metric": trial.suggest_categorical("metric", ["minkowski", "cosine"]),
            "p": trial.suggest_int("p", 1, 3),
        }
        return mean_fold_score(lambda: KNeighborsRegressor(**params), folds, mae_score, use_weights=False)

    study = optuna.create_study(direction="minimize", study_name="knn-regressor-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
