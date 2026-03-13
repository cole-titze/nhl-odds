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
    # "Default": Experiment(
    #     models={
    #         "MLP": mlp(hidden_layer_sizes=(64, 32), max_iter=500),
    #         "LightGBM": lgbm(
    #             n_estimators=640, learning_rate=0.011922, max_depth=4, num_leaves=211, min_child_samples=28
    #         ),
    #         "RF": random_forest(n_estimators=200, max_depth=10),
    #         "KNN": knn(n_neighbors=250, weights="distance"),
    #     },
    #     pipeline=standard_pipeline(k_best=100, pca_components=52),
    #     ensemble=["MLP", "LightGBM", "RF", "KNN"],
    #     calibration="none",
    #     tune=False
    # ),
    "Default": Experiment(
        models={
            "LightGBM": lgbm(),
        },
        pipeline=standard_pipeline(),
        calibration="sigmoid",
        decay=0,
        tune=True,
    ),
    "Deep MLP": Experiment(
        models={
            "MLP": mlp(),
        },
        pipeline=standard_pipeline(),
        calibration="none",
        tune=True,
        decay=0,
        tune_trials=250,
    ),
    "RF": Experiment(
        models={
            "RF": random_forest(),
        },
        pipeline=standard_pipeline(),
        calibration="none",
        tune=True,
        decay=0,
        tune_trials=150,
    ),
    "Deep KNN": Experiment(
        models={
            "KNN": knn(),
        },
        pipeline=standard_pipeline(),
        calibration="none",
        decay=0,
        tune=True,
    ),
    "XGBoost": Experiment(
        models={
            "XGB": xgboost(),
        },
        pipeline=standard_pipeline(),
        calibration="none",
        decay=0,
        tune=True,
    ),
    "Logistic": Experiment(
        models={
            "LR": logistic_regression(),
        },
        pipeline=standard_pipeline(),
        calibration="none",
        decay=0,
        tune=True,
    ),
}

SAVE_EXPERIMENT = "Default"
