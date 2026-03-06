import numpy as np
from lightgbm import LGBMClassifier
from sklearn.decomposition import PCA
from sklearn.feature_selection import SelectKBest, f_classif
from sklearn.neural_network import MLPClassifier
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import MinMaxScaler, StandardScaler


class Ensemble:
    def __init__(self, models: list):
        self._models = models

    def predict_proba(self, X):
        probas = np.array([m.predict_proba(X) for m in self._models])
        return probas.mean(axis=0)

    def predict(self, X):
        return np.argmax(self.predict_proba(X), axis=1)


# --- Define experiments here ---
# Each experiment has:
#   "models"   - dict of model name -> {cls, params}
#   "pipeline" - a sklearn Pipeline for preprocessing
#   "ensemble" - list of model names to combine (or None to skip)

EXPERIMENTS = {
    "Default": {
        "models": {
            "MLP": {
                "cls": MLPClassifier,
                "params": {
                    "hidden_layer_sizes": (64, 32),
                    "max_iter": 500,
                    "early_stopping": True,
                    "random_state": 42,
                },
            },
            "LightGBM": {
                "cls": LGBMClassifier,
                "params": {
                    "n_estimators": 200,
                    "learning_rate": 0.05,
                    "max_depth": 6,
                    "random_state": 42,
                    "verbosity": -1,
                },
            },
        },
        "pipeline": Pipeline([
            ("scaler", StandardScaler()),
            ("minmax", MinMaxScaler()),
            ("select", SelectKBest(f_classif, k=50)),
            ("pca", PCA(n_components=15)),
        ]),
        "ensemble": ["MLP", "LightGBM"],
    },

    "LightGBM Deep": {
        "models": {
            "LightGBM": {
                "cls": LGBMClassifier,
                "params": {
                    "n_estimators": 500,
                    "learning_rate": 0.03,
                    "max_depth": 8,
                    "random_state": 42,
                    "verbosity": -1,
                },
            },
        },
        "pipeline": Pipeline([
            ("scaler", StandardScaler()),
            ("minmax", MinMaxScaler()),
            ("select", SelectKBest(f_classif, k=80)),
            ("pca", PCA(n_components=25)),
        ]),
        "ensemble": None,
    },

    "Wide MLP": {
        "models": {
            "MLP": {
                "cls": MLPClassifier,
                "params": {
                    "hidden_layer_sizes": (128, 64, 32),
                    "max_iter": 800,
                    "early_stopping": True,
                    "random_state": 42,
                },
            },
        },
        "pipeline": Pipeline([
            ("scaler", StandardScaler()),
            ("minmax", MinMaxScaler()),
            ("select", SelectKBest(f_classif, k=50)),
            ("pca", PCA(n_components=15)),
        ]),
        "ensemble": None,
    },
}

SAVE_EXPERIMENT = "Default"
