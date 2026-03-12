# ============================================================================
# EDIT THIS FILE to configure which models run and how they're tuned.
#
# Each Experiment has:
#   models       — dict of name -> ModelConfig  (use mlp() / lgbm() / etc.)
#   pipeline     — sklearn Pipeline              (use standard_pipeline())
#   ensemble     — list of model names to average, or None for single model
#   calibration  — "sigmoid", "isotonic", or "none" (default: "sigmoid")
#   tune         — True to run Optuna tuning during backfill (default: False)
#   tune_trials  — number of Optuna trials (default: 100)
#
# SAVE_EXPERIMENT controls which experiment is used for predictions saved to DB.
# ============================================================================

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

EXPERIMENTS: dict[str, Experiment] = {
    "Default": Experiment(
        models={
            "MLP": mlp(hidden_layer_sizes=(64, 32), max_iter=500),
            "LightGBM": lgbm(n_estimators=200, learning_rate=0.05, max_depth=6),
            "RF": random_forest(n_estimators=200, max_depth=10),
            "KNN": knn(n_neighbors=250, weights="distance"),
        },
        pipeline=standard_pipeline(k_best=100, pca_components=52),
        ensemble=["MLP", "LightGBM", "RF", "KNN"],
        calibration="isotonic",
        tune=True
    ),
    "LightGBM Deep": Experiment(
        models={
            "LightGBM": lgbm(),
        },
        pipeline=standard_pipeline(k_best=80, pca_components=25),
        calibration="sigmoid",
        tune=True
    ),
    "Deep MLP": Experiment(
        models={
            "MLP": mlp(),
        },
        pipeline=standard_pipeline(k_best=126, pca_components=126),
        calibration="none",
        tune=True
    ),
    "RF": Experiment(
        models={
            "RF": random_forest(),
        },
        pipeline=standard_pipeline(k_best=126, pca_components=126),
        calibration="none",
        tune=True
    ),
    "Deep KNN": Experiment(
        models={
            "KNN": knn(),
        },
        pipeline=standard_pipeline(k_best=126, pca_components=80),
        calibration="none",
        tune=True
    ),
    "XGBoost": Experiment(
        models={
            "XGB": xgboost(),
        },
        pipeline=standard_pipeline(),
        calibration="none",
        tune=True
    ),
    "Logistic": Experiment(
        models={
            "LR": logistic_regression(C=1.0),
        },
        pipeline=standard_pipeline(),
        calibration="none",
        tune=False
    ),
}

SAVE_EXPERIMENT = "Default"
