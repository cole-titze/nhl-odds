from xgboost import XGBClassifier

from .types import ModelConfig


def xgboost(
    n_estimators: int = 200,
    learning_rate: float = 0.05,
    max_depth: int = 6,
    min_child_weight: int = 1,
    subsample: float = 1.0,
    colsample_bytree: float = 1.0,
    reg_alpha: float = 0.0,
    reg_lambda: float = 1.0,
    gamma: float = 0.0,
) -> ModelConfig:
    """Create an XGBoost model config."""
    return ModelConfig(
        cls=XGBClassifier,
        params={
            "n_estimators": n_estimators,
            "learning_rate": learning_rate,
            "max_depth": max_depth,
            "min_child_weight": min_child_weight,
            "subsample": subsample,
            "colsample_bytree": colsample_bytree,
            "reg_alpha": reg_alpha,
            "reg_lambda": reg_lambda,
            "gamma": gamma,
            "random_state": 42,
            "verbosity": 0,
            "use_label_encoder": False,
        },
    )


def tune_xgboost(X_train, X_test, y_train, y_test, n_trials, progress_callback, sample_weight=None):
    import optuna
    from sklearn.metrics import log_loss

    def objective(trial):
        params = {
            "n_estimators": trial.suggest_int("n_estimators", 100, 1000),
            "learning_rate": trial.suggest_float("learning_rate", 0.01, 0.3, log=True),
            "max_depth": trial.suggest_int("max_depth", 3, 12),
            "min_child_weight": trial.suggest_int("min_child_weight", 1, 20),
            "subsample": trial.suggest_float("subsample", 0.5, 1.0),
            "colsample_bytree": trial.suggest_float("colsample_bytree", 0.5, 1.0),
            "reg_alpha": trial.suggest_float("reg_alpha", 1e-8, 10.0, log=True),
            "reg_lambda": trial.suggest_float("reg_lambda", 1e-8, 10.0, log=True),
            "gamma": trial.suggest_float("gamma", 1e-8, 5.0, log=True),
            "verbosity": 0,
            "use_label_encoder": False,
            "random_state": 42,
        }
        model = XGBClassifier(**params)
        model.fit(X_train, y_train, sample_weight=sample_weight)
        return log_loss(y_test, model.predict_proba(X_test))

    study = optuna.create_study(direction="minimize", study_name="xgboost-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
