from .folds import log_loss_score, mae_score, mean_fold_score
from .types import ModelConfig


def catboost_regressor(
    iterations: int = 1000,
    learning_rate: float = 0.03,
    depth: int = 6,
    l2_leaf_reg: float = 3.0,
    random_strength: float = 1.0,
    bagging_temperature: float = 1.0,
    border_count: int = 254,
) -> ModelConfig:
    """Create a CatBoost regressor config."""
    from catboost import CatBoostRegressor

    return ModelConfig(
        cls=CatBoostRegressor,
        params={
            "iterations": iterations,
            "learning_rate": learning_rate,
            "depth": depth,
            "l2_leaf_reg": l2_leaf_reg,
            "random_strength": random_strength,
            "bagging_temperature": bagging_temperature,
            "border_count": border_count,
            "random_seed": 42,
            "verbose": False,
            "allow_writing_files": False,
        },
    )


def catboost(
    iterations: int = 1000,
    learning_rate: float = 0.03,
    depth: int = 6,
    l2_leaf_reg: float = 3.0,
    random_strength: float = 1.0,
    bagging_temperature: float = 1.0,
    border_count: int = 254,
) -> ModelConfig:
    """Create a CatBoost model config.

    catboost is a local-only dep (requirements-experimental.txt), imported lazily so the
    production image doesn't need it.
    """
    from catboost import CatBoostClassifier

    return ModelConfig(
        cls=CatBoostClassifier,
        params={
            "iterations": iterations,
            "learning_rate": learning_rate,
            "depth": depth,
            "l2_leaf_reg": l2_leaf_reg,
            "random_strength": random_strength,
            "bagging_temperature": bagging_temperature,
            "border_count": border_count,
            "random_seed": 42,
            "verbose": False,
            "allow_writing_files": False,
        },
    )


def _suggest_params(trial) -> dict:
    return {
        "iterations": trial.suggest_int("iterations", 200, 1500),
        "learning_rate": trial.suggest_float("learning_rate", 0.005, 0.2, log=True),
        "depth": trial.suggest_int("depth", 3, 10),
        "l2_leaf_reg": trial.suggest_float("l2_leaf_reg", 1e-2, 30.0, log=True),
        "random_strength": trial.suggest_float("random_strength", 1e-3, 10.0, log=True),
        "bagging_temperature": trial.suggest_float("bagging_temperature", 0.0, 5.0),
        "border_count": trial.suggest_categorical("border_count", [32, 64, 128, 254]),
        "random_seed": 42,
        "verbose": False,
        "allow_writing_files": False,
        # One thread per trial — Optuna already runs trials in parallel
        "thread_count": 1,
    }


def tune_catboost(folds, n_trials, progress_callback):
    import optuna
    from catboost import CatBoostClassifier

    def objective(trial):
        params = _suggest_params(trial)
        return mean_fold_score(lambda: CatBoostClassifier(**params), folds, log_loss_score)

    study = optuna.create_study(direction="minimize", study_name="catboost-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study


def tune_catboost_regressor(folds, n_trials, progress_callback):
    import optuna
    from catboost import CatBoostRegressor

    def objective(trial):
        params = _suggest_params(trial)
        return mean_fold_score(lambda: CatBoostRegressor(**params), folds, mae_score)

    study = optuna.create_study(direction="minimize", study_name="catboost-regressor-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
