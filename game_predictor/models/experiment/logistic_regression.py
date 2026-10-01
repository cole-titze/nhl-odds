from sklearn.linear_model import LogisticRegression as LR

from .folds import log_loss_score, mean_fold_score
from .types import ModelConfig


def logistic_regression(
    C: float = 1.0,
    l1_ratio: float = 0.0,
) -> ModelConfig:
    """Create a Logistic Regression model config."""
    params: dict = {"C": C, "l1_ratio": l1_ratio, "solver": "saga", "max_iter": 2000, "random_state": 42}
    return ModelConfig(cls=LR, params=params)


def tune_logistic_regression(folds, n_trials, progress_callback):
    import optuna

    def objective(trial):
        params = {
            "C": trial.suggest_float("C", 1e-4, 100.0, log=True),
            "l1_ratio": trial.suggest_float("l1_ratio", 0.0, 1.0),
            "solver": "saga",
            "max_iter": 2000,
            "random_state": 42,
        }
        return mean_fold_score(lambda: LR(**params), folds, log_loss_score)

    study = optuna.create_study(direction="minimize", study_name="lr-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
