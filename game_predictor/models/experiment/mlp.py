from sklearn.neural_network import MLPClassifier, MLPRegressor

from .types import ModelConfig


def mlp_regressor(
    hidden_layer_sizes: tuple[int, ...] = (64, 32),
    max_iter: int = 500,
    learning_rate_init: float = 1e-3,
    alpha: float = 1e-4,
    activation: str = "relu",
) -> ModelConfig:
    """Create an MLP regressor config."""
    return ModelConfig(
        cls=MLPRegressor,
        params={
            "hidden_layer_sizes": hidden_layer_sizes,
            "max_iter": max_iter,
            "learning_rate_init": learning_rate_init,
            "alpha": alpha,
            "activation": activation,
            "early_stopping": True,
            "random_state": 42,
        },
    )


def mlp(
    hidden_layer_sizes: tuple[int, ...] = (64, 32),
    max_iter: int = 500,
    learning_rate_init: float = 1e-3,
    alpha: float = 1e-4,
    activation: str = "relu",
) -> ModelConfig:
    """Create an MLP model config."""
    return ModelConfig(
        cls=MLPClassifier,
        params={
            "hidden_layer_sizes": hidden_layer_sizes,
            "max_iter": max_iter,
            "learning_rate_init": learning_rate_init,
            "alpha": alpha,
            "activation": activation,
            "early_stopping": True,
            "random_state": 42,
        },
    )


def tune_mlp(X_train, X_test, y_train, y_test, n_trials, progress_callback, sample_weight=None):
    import optuna
    from sklearn.metrics import log_loss

    def objective(trial):
        n_layers = trial.suggest_int("n_layers", 1, 3)
        layers = tuple(trial.suggest_int(f"layer_{i}", 16, 256) for i in range(n_layers))
        params = {
            "hidden_layer_sizes": layers,
            "max_iter": trial.suggest_int("max_iter", 200, 1000),
            "learning_rate_init": trial.suggest_float("learning_rate_init", 1e-4, 1e-1, log=True),
            "alpha": trial.suggest_float("alpha", 1e-6, 1e-1, log=True),
            "activation": trial.suggest_categorical("activation", ["relu", "tanh"]),
            "early_stopping": True,
            "random_state": 42,
        }
        model = MLPClassifier(**params)
        model.fit(X_train, y_train)
        return log_loss(y_test, model.predict_proba(X_test))

    study = optuna.create_study(direction="minimize", study_name="mlp-tuning")
    # n_jobs=1: MLPClassifier already uses all cores internally
    study.optimize(objective, n_trials=n_trials, n_jobs=1, callbacks=[progress_callback])
    return study


def tune_mlp_regressor(X_train, X_test, y_train, y_test, n_trials, progress_callback, sample_weight=None):
    import optuna
    from sklearn.metrics import mean_absolute_error

    def objective(trial):
        n_layers = trial.suggest_int("n_layers", 1, 3)
        layers = tuple(trial.suggest_int(f"layer_{i}", 16, 256) for i in range(n_layers))
        params = {
            "hidden_layer_sizes": layers,
            "max_iter": trial.suggest_int("max_iter", 200, 1000),
            "learning_rate_init": trial.suggest_float("learning_rate_init", 1e-4, 1e-1, log=True),
            "alpha": trial.suggest_float("alpha", 1e-6, 1e-1, log=True),
            "activation": trial.suggest_categorical("activation", ["relu", "tanh"]),
            "early_stopping": True,
            "random_state": 42,
        }
        model = MLPRegressor(**params)
        model.fit(X_train, y_train)
        return mean_absolute_error(y_test, model.predict(X_test))

    study = optuna.create_study(direction="minimize", study_name="mlp-regressor-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=1, callbacks=[progress_callback])
    return study


def mlp_params_from_study(study) -> dict:
    """Convert Optuna study best_params to MLPClassifier constructor params."""
    params = study.best_params.copy()
    n_layers = params.pop("n_layers")
    layers = tuple(params.pop(f"layer_{i}") for i in range(n_layers))
    params["hidden_layer_sizes"] = layers
    params["early_stopping"] = True
    params["random_state"] = 42
    return params


def mlp_regressor_params_from_study(study) -> dict:
    """Convert Optuna study best_params to MLPRegressor constructor params."""
    params = study.best_params.copy()
    n_layers = params.pop("n_layers")
    layers = tuple(params.pop(f"layer_{i}") for i in range(n_layers))
    params["hidden_layer_sizes"] = layers
    params["early_stopping"] = True
    params["random_state"] = 42
    return params
