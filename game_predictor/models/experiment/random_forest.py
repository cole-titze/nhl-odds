from __future__ import annotations

from sklearn.ensemble import RandomForestClassifier, RandomForestRegressor

from .folds import log_loss_score, mae_score, mean_fold_score
from .types import ModelConfig


def random_forest_regressor(
    n_estimators: int = 200,
    max_depth: int | None = None,
    min_samples_split: int = 2,
    min_samples_leaf: int = 1,
    max_features: str | None = "sqrt",
) -> ModelConfig:
    """Create a Random Forest regressor config."""
    return ModelConfig(
        cls=RandomForestRegressor,
        params={
            "n_estimators": n_estimators,
            "max_depth": max_depth,
            "min_samples_split": min_samples_split,
            "min_samples_leaf": min_samples_leaf,
            "max_features": max_features,
            "random_state": 42,
        },
    )


def random_forest(
    n_estimators: int = 200,
    max_depth: int | None = None,
    min_samples_split: int = 2,
    min_samples_leaf: int = 1,
    max_features: str | None = "sqrt",
) -> ModelConfig:
    """Create a Random Forest model config."""
    return ModelConfig(
        cls=RandomForestClassifier,
        params={
            "n_estimators": n_estimators,
            "max_depth": max_depth,
            "min_samples_split": min_samples_split,
            "min_samples_leaf": min_samples_leaf,
            "max_features": max_features,
            "random_state": 42,
        },
    )


def tune_random_forest(folds, n_trials, progress_callback):
    import optuna

    def objective(trial):
        params = {
            "n_estimators": trial.suggest_int("n_estimators", 50, 500),
            "max_depth": trial.suggest_int("max_depth", 3, 30),
            "min_samples_split": trial.suggest_int("min_samples_split", 2, 20),
            "min_samples_leaf": trial.suggest_int("min_samples_leaf", 1, 20),
            "max_features": trial.suggest_categorical("max_features", ["sqrt", "log2", None]),
            "random_state": 42,
        }
        return mean_fold_score(lambda: RandomForestClassifier(**params), folds, log_loss_score)

    study = optuna.create_study(direction="minimize", study_name="rf-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study


def tune_random_forest_regressor(folds, n_trials, progress_callback):
    import optuna

    def objective(trial):
        params = {
            "n_estimators": trial.suggest_int("n_estimators", 50, 500),
            "max_depth": trial.suggest_int("max_depth", 3, 30),
            "min_samples_split": trial.suggest_int("min_samples_split", 2, 20),
            "min_samples_leaf": trial.suggest_int("min_samples_leaf", 1, 20),
            "max_features": trial.suggest_categorical("max_features", ["sqrt", "log2", None]),
            "random_state": 42,
        }
        return mean_fold_score(lambda: RandomForestRegressor(**params), folds, mae_score)

    study = optuna.create_study(direction="minimize", study_name="rf-regressor-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
