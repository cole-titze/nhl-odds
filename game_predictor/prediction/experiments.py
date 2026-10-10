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
#
# REGRESSION_EXPERIMENTS define spread/total models (use RegressionExperiment).
# Set SAVE_SPREAD_EXPERIMENT / SAVE_TOTAL_EXPERIMENT to None to disable.
# ============================================================================

import importlib.util

from ..models.experiment import (
    Experiment,
    RegressionExperiment,
    catboost,
    knn,
    lgbm,
    lgbm_regressor,
    logistic_regression,
    mlp,
    mlp_regressor,
    random_forest,
    random_forest_regressor,
    standard_pipeline,
    tabm,
    xgboost,
    xgboost_regressor,
)

EXPERIMENTS: dict[str, Experiment] = {
    # LR + KNN stack: ties Stack6 in walk-forward evals (log loss +0.0002 on 2019-22, +0.0003 on 2023-26,
    # both within noise) for about a tenth of the training compute. LR carries the stack.
    "Default": Experiment(
        models={
            "KNN": knn(n_neighbors=152, weights="distance", metric="minkowski", p=2),
            "LR": logistic_regression(C=0.0768651, l1_ratio=1.0),
        },
        pipeline=standard_pipeline(k_best=99, pca_components=90),
        ensemble=["KNN", "LR"],
        calibration="none",
        decay=0.08,
        stack=True,
        tune=False,
    ),
    # Previous Default: six-model stack
    "Stack6": Experiment(
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
            "LR": logistic_regression(C=0.0768651, l1_ratio=1.0),
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
    "Compressed MLP": Experiment(
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
            "LR": logistic_regression(C=0.0768651, l1_ratio=1.0),
        },
        pipeline=standard_pipeline(k_best=124, pca_components=98),
        calibration="none",
        decay=0.0,
        tune=False,
    ),
    "Tuned Boosters": Experiment(
        models={
            "LightGBM": lgbm(
                n_estimators=970,
                learning_rate=0.0201986,
                max_depth=5,
                num_leaves=156,
                min_child_samples=83,
                subsample=0.804052,
                colsample_bytree=0.843525,
                reg_alpha=1.71649e-06,
                reg_lambda=2.14657e-06,
            ),
            "XGB": xgboost(
                n_estimators=252,
                learning_rate=0.0188796,
                max_depth=7,
                min_child_weight=7,
                subsample=0.724605,
                colsample_bytree=0.873154,
                reg_alpha=0.00101763,
                reg_lambda=0.0569719,
                gamma=0.0032077,
            ),
            "MLP": mlp(
                hidden_layer_sizes=(47, 60, 174),
                max_iter=905,
                learning_rate_init=0.00246339,
                alpha=4.73291e-06,
                activation="tanh",
            ),
        },
        pipeline=standard_pipeline(k_best=83, pca_components=56),
        ensemble=["LightGBM", "XGB", "MLP"],
        stack=True,
        calibration="none",
        decay=0.01789,
        tune=False,
    ),
}

# Local-only models: their deps are in requirements-experimental.txt, not in the predictor
# image, so they only register when installed.
if importlib.util.find_spec("catboost"):
    EXPERIMENTS["CatBoost"] = Experiment(
        models={
            "CatBoost": catboost(
                iterations=386,
                learning_rate=0.0250923,
                depth=5,
                l2_leaf_reg=0.949754,
                random_strength=0.483826,
                bagging_temperature=3.83555,
                border_count=64,
            ),
        },
        pipeline=standard_pipeline(k_best=63, pca_components=45),
        calibration="sigmoid",
        decay=0.006325,
        tune=False,
    )

if importlib.util.find_spec("pytabkit"):
    # Untuned pytabkit defaults: 30 Optuna trials scored no better (they overfit the tuning seasons)
    EXPERIMENTS["TabM"] = Experiment(
        models={
            "TabM": tabm(),
        },
        pipeline=standard_pipeline(k_best=99, pca_components=None),
        calibration="none",
        decay=0.0,
        tune=False,
    )

SAVE_EXPERIMENT = "Default"

# --- Regression experiments (spread / over-under) ---

REGRESSION_EXPERIMENTS: dict[str, RegressionExperiment] = {
    "Spread": RegressionExperiment(
        models={
            "LightGBM": lgbm_regressor(n_estimators=300, learning_rate=0.01, max_depth=7),
            "XGB": xgboost_regressor(n_estimators=500, learning_rate=0.01, max_depth=5),
            "MLP": mlp_regressor(hidden_layer_sizes=(128, 64)),
            "RF": random_forest_regressor(n_estimators=200, max_depth=20),
        },
        pipeline=standard_pipeline(k_best=80, pca_components=60, regression=True),
        ensemble=["LightGBM", "XGB", "MLP", "RF"],
        target="spread",
        decay=0.08,
    ),
    "Total": RegressionExperiment(
        models={
            "LightGBM": lgbm_regressor(n_estimators=300, learning_rate=0.01, max_depth=7),
            "XGB": xgboost_regressor(n_estimators=500, learning_rate=0.01, max_depth=5),
            "MLP": mlp_regressor(hidden_layer_sizes=(128, 64)),
            "RF": random_forest_regressor(n_estimators=200, max_depth=20),
        },
        pipeline=standard_pipeline(k_best=80, pca_components=60, regression=True),
        ensemble=["LightGBM", "XGB", "MLP", "RF"],
        target="total",
        decay=0.08,
    ),
}

SAVE_SPREAD_EXPERIMENT = "Spread"
SAVE_TOTAL_EXPERIMENT = "Total"
