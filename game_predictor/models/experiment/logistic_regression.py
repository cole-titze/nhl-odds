from sklearn.linear_model import LogisticRegression as LR

from .types import ModelConfig


def logistic_regression(
    C: float = 1.0,
    l1_ratio: float = 0.0,
) -> ModelConfig:
    """Create a Logistic Regression model config."""
    params: dict = {"C": C, "l1_ratio": l1_ratio, "solver": "saga", "max_iter": 2000, "random_state": 42}
    return ModelConfig(cls=LR, params=params)


def tune_logistic_regression(X_train, X_test, y_train, y_test, n_trials, progress_callback, sample_weight=None):
    import optuna
    from sklearn.metrics import log_loss

    def objective(trial):
        params = {
            "C": trial.suggest_float("C", 1e-4, 100.0, log=True),
            "l1_ratio": trial.suggest_float("l1_ratio", 0.0, 1.0),
            "solver": "saga",
            "max_iter": 2000,
            "random_state": 42,
        }
        model = LR(**params)
        model.fit(X_train, y_train, sample_weight=sample_weight)
        return log_loss(y_test, model.predict_proba(X_test))

    study = optuna.create_study(direction="minimize", study_name="lr-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
