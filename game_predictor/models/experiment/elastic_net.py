from sklearn.linear_model import ElasticNet

from .folds import mae_score, mean_fold_score
from .types import ModelConfig


def elastic_net_regressor(
    alpha: float = 1.0,
    l1_ratio: float = 0.5,
) -> ModelConfig:
    """Create an ElasticNet (linear regression with L1/L2 penalty) config — the regression twin of
    logistic_regression(). l1_ratio=0 is ridge, 1 is lasso."""
    return ModelConfig(
        cls=ElasticNet,
        params={"alpha": alpha, "l1_ratio": l1_ratio, "max_iter": 5000, "random_state": 42},
    )


def tune_elastic_net_regressor(folds, n_trials, progress_callback):
    import optuna

    def objective(trial):
        params = {
            "alpha": trial.suggest_float("alpha", 1e-4, 10.0, log=True),
            "l1_ratio": trial.suggest_float("l1_ratio", 0.0, 1.0),
            "max_iter": 5000,
            "random_state": 42,
        }
        return mean_fold_score(lambda: ElasticNet(**params), folds, mae_score)

    study = optuna.create_study(direction="minimize", study_name="elastic-net-regressor-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
