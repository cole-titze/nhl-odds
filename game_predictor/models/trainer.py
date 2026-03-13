import inspect

from sklearn.ensemble import StackingClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, log_loss

from .ensemble import Ensemble
from .experiment import ModelConfig


def build_models(model_configs: dict[str, ModelConfig]) -> dict:
    return {name: cfg.build() for name, cfg in model_configs.items()}


def _fit(model, X, y, sample_weight=None):
    if sample_weight is not None and "sample_weight" in inspect.signature(model.fit).parameters:
        model.fit(X, y, sample_weight=sample_weight)
    else:
        model.fit(X, y)


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

    for name, model in models.items():
        _fit(model, X_train, y_train, sample_weight)

        y_proba = model.predict_proba(X_test)
        y_pred = model.predict(X_test)

        results[name] = {
            "model": model,
            "accuracy": accuracy_score(y_test, y_pred),
            "log_loss": log_loss(y_test, y_proba),
        }

    if ensemble_names and len(ensemble_names) > 1:
        if stack:
            # StackingClassifier needs unfitted estimators — clone from already-fitted models
            from sklearn.base import clone

            estimators = [(n, clone(results[n]["model"])) for n in ensemble_names]
            stacker = StackingClassifier(
                estimators=estimators,
                final_estimator=LogisticRegression(),
                cv=5,
                stack_method="predict_proba",
                n_jobs=-1,
            )
            stacker.fit(X_train, y_train)
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
            "accuracy": accuracy_score(y_test, y_pred),
            "log_loss": log_loss(y_test, y_proba),
        }

    return results
