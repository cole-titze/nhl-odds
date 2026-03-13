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
            "MLP": mlp(hidden_layer_sizes=(212,), learning_rate_init=0.00112711, alpha=0.0367419, activation="relu"),
            "LightGBM": lgbm(
                n_estimators=260,
                learning_rate=0.0133063,
                max_depth=7,
                num_leaves=240,
                min_child_samples=5,
                subsample=0.788132,
                colsample_bytree=0.644878,
                reg_alpha=8.03915e-06,
                reg_lambda=0.0487625,
            ),
            "RF": random_forest(
                n_estimators=184, max_depth=26, min_samples_split=11, min_samples_leaf=3, max_features="sqrt"
            ),
            "KNN": knn(n_neighbors=152, weights="distance", metric="minkowski", p=2),
            "XGB": xgboost(
                n_estimators=830,
                learning_rate=0.0147956,
                max_depth=4,
                min_child_weight=1,
                subsample=0.901271,
                colsample_bytree=0.799057,
                reg_alpha=3.51218e-06,
                reg_lambda=0.0333008,
                gamma=2.56812e-07,
            ),
            "LR": logistic_regression(C=0.0768651, penalty="l1"),
        },
        pipeline=standard_pipeline(k_best=99, pca_components=90),
        ensemble=["MLP", "LightGBM", "RF", "KNN", "XGB", "LR"],
        calibration="none",
        decay=0.08,
        stack=True,
        tune=False,
    ),
    "LightGBM": Experiment(
        models={
            "LightGBM": lgbm(
                n_estimators=260,
                learning_rate=0.0133063,
                max_depth=7,
                num_leaves=240,
                min_child_samples=5,
                subsample=0.788132,
                colsample_bytree=0.644878,
                reg_alpha=8.03915e-06,
                reg_lambda=0.0487625,
            ),
        },
        pipeline=standard_pipeline(k_best=99, pca_components=90),
        calibration="none",
        decay=0.1055,
        tune=False,
    ),
    "Deep MLP": Experiment(
        models={
            "MLP": mlp(hidden_layer_sizes=(212,), learning_rate_init=0.00112711, alpha=0.0367419, activation="relu"),
        },
        pipeline=standard_pipeline(k_best=20, pca_components=6),
        calibration="none",
        decay=0.07648,
        tune=False,
    ),
    "RF": Experiment(
        models={
            "RF": random_forest(
                n_estimators=184, max_depth=26, min_samples_split=11, min_samples_leaf=3, max_features="sqrt"
            ),
        },
        pipeline=standard_pipeline(k_best=31, pca_components=21),
        calibration="none",
        decay=0.1507,
        tune=False,
    ),
    "Deep KNN": Experiment(
        models={
            "KNN": knn(n_neighbors=152, weights="distance", metric="minkowski", p=2),
        },
        pipeline=standard_pipeline(k_best=62, pca_components=20),
        calibration="none",
        decay=0.4337,
        tune=False,
    ),
    "XGBoost": Experiment(
        models={
            "XGB": xgboost(
                n_estimators=830,
                learning_rate=0.0147956,
                max_depth=4,
                min_child_weight=1,
                subsample=0.901271,
                colsample_bytree=0.799057,
                reg_alpha=3.51218e-06,
                reg_lambda=0.0333008,
                gamma=2.56812e-07,
            ),
        },
        pipeline=standard_pipeline(k_best=88, pca_components=71),
        calibration="sigmoid",
        decay=0.02728,
        tune=False,
    ),
    "Logistic": Experiment(
        models={
            "LR": logistic_regression(C=0.0768651, penalty="l1"),
        },
        pipeline=standard_pipeline(k_best=124, pca_components=98),
        calibration="none",
        decay=0.0008893,
        tune=False,
    ),
}

SAVE_EXPERIMENT = "Default"
