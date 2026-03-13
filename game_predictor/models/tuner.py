import optuna

from .experiment.knn import tune_knn
from .experiment.lgbm import tune_lgbm
from .experiment.logistic_regression import tune_logistic_regression
from .experiment.mlp import mlp_params_from_study, tune_mlp
from .experiment.random_forest import tune_random_forest
from .experiment.xgboost import tune_xgboost

optuna.logging.set_verbosity(optuna.logging.WARNING)

# Maps model class name -> tune function
TUNERS = {
    "LGBMClassifier": tune_lgbm,
    "XGBClassifier": tune_xgboost,
    "KNeighborsClassifier": tune_knn,
    "MLPClassifier": tune_mlp,
    "RandomForestClassifier": tune_random_forest,
    "LogisticRegression": tune_logistic_regression,
}


# Maps model class name -> function that converts study.best_params to constructor params.
# Only needed when Optuna search params differ from the model's __init__ args.
PARAM_CONVERTERS = {
    "MLPClassifier": mlp_params_from_study,
}


def progress_callback(n_trials: int):
    import threading

    lock = threading.Lock()
    state = {"completed": 0}

    def callback(study, trial):
        with lock:
            state["completed"] += 1
            n = state["completed"]
            if n % 20 == 0 or n == n_trials:
                print(f"    Trial {n}/{n_trials} — best log loss: {study.best_value:.4f}")

    return callback


def print_best_params(study: optuna.Study, model_name: str):
    """Print the best parameters from an Optuna study."""
    print(f"    Best log loss: {study.best_value:.4f} (trial #{study.best_trial.number})")
    print("    Best parameters:")
    converter = PARAM_CONVERTERS.get(model_name)
    params = converter(study) if converter else study.best_params
    for key, value in params.items():
        if isinstance(value, float):
            print(f"      {key}: {value:.6f}")
        else:
            print(f"      {key}: {value}")
