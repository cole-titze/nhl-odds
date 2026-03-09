import numpy as np

from ..models.experiment import (
    Experiment,
    knn,
    lgbm,
    logistic_regression,
    mlp,
    random_forest,
    standard_pipeline,
    xgboost,
)


class Ensemble:
    def __init__(self, models: list):
        self._models = models

    def predict_proba(self, X):
        probas = np.array([m.predict_proba(X) for m in self._models])
        return probas.mean(axis=0)

    def predict(self, X):
        return np.argmax(self.predict_proba(X), axis=1)


# ---------------------------------------------------------------------------
# Define experiments here.
# Each Experiment has typed fields with intellisense:
#   models   - dict of name -> ModelConfig  (use mlp() / lgbm() helpers)
#   pipeline - sklearn Pipeline              (use standard_pipeline() helper)
#   ensemble - list of model names to average, or None for single model
# ---------------------------------------------------------------------------

EXPERIMENTS: dict[str, Experiment] = {
    "Default": Experiment(
        models={
            "MLP": mlp(hidden_layer_sizes=(64, 32), max_iter=500),
            "LightGBM": lgbm(n_estimators=200, learning_rate=0.05, max_depth=6),
            "RF": random_forest(n_estimators=200, max_depth=10),
            "KNN": knn(n_neighbors=15, weights="distance"),
        },
        pipeline=standard_pipeline(k_best=50, pca_components=15),
        ensemble=["MLP", "LightGBM", "RF", "KNN"],
    ),
    "LightGBM Deep": Experiment(
        models={
            "LightGBM": lgbm(n_estimators=500, learning_rate=0.03, max_depth=8),
        },
        pipeline=standard_pipeline(k_best=80, pca_components=25),
    ),
    "Wide MLP": Experiment(
        models={
            "MLP": mlp(hidden_layer_sizes=(128, 64, 32), max_iter=800),
        },
        pipeline=standard_pipeline(k_best=50, pca_components=15),
    ),
    "Deep MLP": Experiment(
        models={
            "MLP": mlp(hidden_layer_sizes=(128, 64, 32), max_iter=800),
        },
        pipeline=standard_pipeline(k_best=126, pca_components=126),
    ),
    "RF": Experiment(
        models={
            "RF": random_forest(n_estimators=250, max_depth=10),
        },
        pipeline=standard_pipeline(k_best=126, pca_components=126),
    ),
    "Shallow KNN": Experiment(
        models={
            "KNN": knn(n_neighbors=15, weights="distance"),
        },
        pipeline=standard_pipeline(k_best=100, pca_components=32),
    ),
    "Deep KNN": Experiment(
        models={
            "KNN": knn(n_neighbors=250, weights="distance"),
        },
        pipeline=standard_pipeline(k_best=126, pca_components=80),
    ),
    "XGBoost": Experiment(
        models={
            "XGB": xgboost(n_estimators=200, learning_rate=0.05, max_depth=6),
        },
        pipeline=standard_pipeline(),
    ),
    "Logistic": Experiment(
        models={
            "LR": logistic_regression(C=1.0),
        },
        pipeline=standard_pipeline(),
    ),
}

SAVE_EXPERIMENT = "Default"
