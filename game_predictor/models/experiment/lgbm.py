from lightgbm import LGBMClassifier, LGBMRegressor

from .types import ModelConfig


def lgbm_regressor(
    n_estimators: int = 200,
    learning_rate: float = 0.05,
    max_depth: int = 6,
    num_leaves: int = 31,
    min_child_samples: int = 20,
    subsample: float = 1.0,
    colsample_bytree: float = 1.0,
    reg_alpha: float = 0.0,
    reg_lambda: float = 0.0,
) -> ModelConfig:
    """Create a LightGBM regressor config."""
    return ModelConfig(
        cls=LGBMRegressor,
        params={
            "n_estimators": n_estimators,
            "learning_rate": learning_rate,
            "max_depth": max_depth,
            "num_leaves": num_leaves,
            "min_child_samples": min_child_samples,
            "subsample": subsample,
            "colsample_bytree": colsample_bytree,
            "reg_alpha": reg_alpha,
            "reg_lambda": reg_lambda,
            "random_state": 42,
            "verbosity": -1,
        },
    )


def lgbm(
    n_estimators: int = 200,
    learning_rate: float = 0.05,
    max_depth: int = 6,
    num_leaves: int = 31,
    min_child_samples: int = 20,
    subsample: float = 1.0,
    colsample_bytree: float = 1.0,
    reg_alpha: float = 0.0,
    reg_lambda: float = 0.0,
) -> ModelConfig:
    """Create a LightGBM model config."""
    return ModelConfig(
        cls=LGBMClassifier,
        params={
            "n_estimators": n_estimators,
            "learning_rate": learning_rate,
            "max_depth": max_depth,
            "num_leaves": num_leaves,
            "min_child_samples": min_child_samples,
            "subsample": subsample,
            "colsample_bytree": colsample_bytree,
            "reg_alpha": reg_alpha,
            "reg_lambda": reg_lambda,
            "random_state": 42,
            "verbosity": -1,
        },
    )


def tune_lgbm(X_train, X_test, y_train, y_test, n_trials, progress_callback, sample_weight=None):
    import optuna
    from sklearn.metrics import log_loss

    def objective(trial):
        params = {
            "n_estimators": trial.suggest_int("n_estimators", 100, 1000),
            "learning_rate": trial.suggest_float("learning_rate", 0.01, 0.3, log=True),
            "max_depth": trial.suggest_int("max_depth", 3, 12),
            "num_leaves": trial.suggest_int("num_leaves", 15, 255),
            "min_child_samples": trial.suggest_int("min_child_samples", 5, 100),
            "subsample": trial.suggest_float("subsample", 0.5, 1.0),
            "colsample_bytree": trial.suggest_float("colsample_bytree", 0.5, 1.0),
            "reg_alpha": trial.suggest_float("reg_alpha", 1e-8, 10.0, log=True),
            "reg_lambda": trial.suggest_float("reg_lambda", 1e-8, 10.0, log=True),
            "verbosity": -1,
            "random_state": 42,
        }
        model = LGBMClassifier(**params)
        model.fit(X_train, y_train, sample_weight=sample_weight)
        return log_loss(y_test, model.predict_proba(X_test))

    study = optuna.create_study(direction="minimize", study_name="lgbm-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=-1, callbacks=[progress_callback])
    return study
