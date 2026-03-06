from sklearn.metrics import accuracy_score, log_loss

from ..prediction.predictor import Ensemble


def build_models(model_configs: dict) -> dict:
    models = {}
    for name, cfg in model_configs.items():
        models[name] = cfg["cls"](**cfg["params"])
    return models


def train_and_evaluate(
    models: dict, X_train, X_test, y_train, y_test, ensemble_names: list | None
) -> dict:
    results = {}

    for name, model in models.items():
        model.fit(X_train, y_train)

        y_proba = model.predict_proba(X_test)
        y_pred = model.predict(X_test)

        results[name] = {
            "model": model,
            "accuracy": accuracy_score(y_test, y_pred),
            "log_loss": log_loss(y_test, y_proba),
        }

    if ensemble_names and len(ensemble_names) > 1:
        ensemble_models = [results[n]["model"] for n in ensemble_names]
        ensemble = Ensemble(ensemble_models)
        y_proba = ensemble.predict_proba(X_test)
        y_pred = ensemble.predict(X_test)

        results["Ensemble"] = {
            "model": ensemble,
            "accuracy": accuracy_score(y_test, y_pred),
            "log_loss": log_loss(y_test, y_proba),
        }

    return results
