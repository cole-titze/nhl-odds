from joblib import Parallel, delayed
from sklearn.metrics import accuracy_score, log_loss

from .ensemble import Ensemble, WeightedStackingClassifier
from .experiment import ModelConfig
from .experiment.folds import fit_model


def build_models(model_configs: dict[str, ModelConfig]) -> dict:
    return {name: cfg.build() for name, cfg in model_configs.items()}


def fit_models(models: dict, X, y, sample_weight=None) -> dict:
    """Fit every model at once. Returns the fitted models (copies, not the ones passed in)."""
    fitted = Parallel(n_jobs=-1)(delayed(fit_model)(model, X, y, sample_weight) for model in models.values())
    return dict(zip(models, fitted))


def train_and_evaluate(
    models: dict,
    X_train,
    X_test,
    y_train,
    y_test,
    ensemble_names: list | None,
    stack: bool = False,
    sample_weight=None,
) -> dict:
    results = {}

    for name, model in fit_models(models, X_train, y_train, sample_weight).items():
        y_proba = model.predict_proba(X_test)
        y_pred = model.predict(X_test)

        results[name] = {
            "model": model,
            "proba": y_proba,
            "accuracy": accuracy_score(y_test, y_pred),
            "log_loss": log_loss(y_test, y_proba),
        }

    if ensemble_names and len(ensemble_names) > 1:
        if stack:
            # The stacker refits its own copies — clone unfitted versions of the already-fitted models
            from sklearn.base import clone

            estimators = [(n, clone(results[n]["model"])) for n in ensemble_names]
            stacker = WeightedStackingClassifier(estimators=estimators)
            fitted = [results[n]["model"] for n in ensemble_names]
            stacker.fit(X_train, y_train, sample_weight=sample_weight, fitted_estimators=fitted)
            y_proba = stacker.predict_proba(X_test)
            y_pred = stacker.predict(X_test)
            ensemble_model = stacker
        else:
            ensemble_models = [results[n]["model"] for n in ensemble_names]
            ensemble_model = Ensemble(models=ensemble_models)
            ensemble_model.fit(X_train, y_train)
            y_proba = ensemble_model.predict_proba(X_test)
            y_pred = ensemble_model.predict(X_test)

        results["Ensemble"] = {
            "model": ensemble_model,
            "proba": y_proba,
            "accuracy": accuracy_score(y_test, y_pred),
            "log_loss": log_loss(y_test, y_proba),
        }

    return results
