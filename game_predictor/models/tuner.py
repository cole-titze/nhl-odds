import optuna
from sklearn.metrics import log_loss

optuna.logging.set_verbosity(optuna.logging.WARNING)


def tune_lgbm(X_train, X_test, y_train, y_test, n_trials: int = 100) -> optuna.Study:
    """Run Optuna study to tune LightGBM hyperparameters."""
    from lightgbm import LGBMClassifier

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
        model.fit(X_train, y_train)
        y_proba = model.predict_proba(X_test)
        return log_loss(y_test, y_proba)

    study = optuna.create_study(direction="minimize", study_name="lgbm-tuning")
    study.optimize(objective, n_trials=n_trials, callbacks=[_progress_callback(n_trials)])
    return study


def tune_xgboost(X_train, X_test, y_train, y_test, n_trials: int = 100) -> optuna.Study:
    """Run Optuna study to tune XGBoost hyperparameters."""
    from xgboost import XGBClassifier

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
        model.fit(X_train, y_train)
        y_proba = model.predict_proba(X_test)
        return log_loss(y_test, y_proba)

    study = optuna.create_study(direction="minimize", study_name="xgboost-tuning")
    study.optimize(objective, n_trials=n_trials, callbacks=[_progress_callback(n_trials)])
    return study


def _progress_callback(n_trials: int):
    def callback(study, trial):
        n = trial.number + 1
        best = study.best_value
        if n % 20 == 0 or n == n_trials:
            print(f"    Trial {n}/{n_trials} — best log loss: {best:.4f}")

    return callback


def print_best_params(study: optuna.Study, model_name: str):
    """Print the best parameters from an Optuna study."""
    print(f"\n{'=' * 60}")
    print(f"  Optuna Results — {model_name}")
    print(f"{'=' * 60}")
    print(f"  Best log loss: {study.best_value:.4f}")
    print(f"  Best trial:    #{study.best_trial.number}")
    print(f"\n  Best parameters:")
    for key, value in study.best_params.items():
        if isinstance(value, float):
            print(f"    {key}: {value:.6f}")
        else:
            print(f"    {key}: {value}")

    # Print as copy-paste ready code
    print(f"\n  Copy-paste for experiment definition:")
    if model_name == "xgboost":
        print(f'    xgboost(')
    else:
        print(f'    lgbm(')
    for key, value in study.best_params.items():
        if isinstance(value, float):
            print(f"        {key}={value:.6f},")
        else:
            print(f"        {key}={value},")
    print(f"    )")
