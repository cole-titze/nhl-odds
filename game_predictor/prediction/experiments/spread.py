# ============================================================================
# EDIT THIS FILE to configure spread (goal differential) models (use RegressionExperiment).
#
# These share the moneyline model's features; only the models/pipeline differ.
# SAVE_SPREAD_EXPERIMENT controls which experiment is saved to DB; set it to None to disable.
# ============================================================================

from ...models.experiment import (
    RegressionExperiment,
    elastic_net_regressor,
    lgbm_regressor,
    mlp_regressor,
    random_forest_regressor,
    standard_pipeline,
    xgboost_regressor,
)

SPREAD_EXPERIMENTS: dict[str, RegressionExperiment] = {
    # Tuned ElasticNet (penalized linear regression). Walk-forward MAE 2023-26: 2.135 vs 2.140 for the old
    # four-model ensemble (within noise, 4/4 seasons better) and 2.178 for always guessing the median. Untuned
    # TabM scored 2.131; no combo of models beat TabM alone.
    "ElasticNet": RegressionExperiment(
        models={
            "ElasticNet": elastic_net_regressor(alpha=0.0406956, l1_ratio=0.651876),
        },
        pipeline=standard_pipeline(k_best=43, pca_components=37, regression=True),
        target="spread",
        decay=0.9894,
    ),
    # Previous default: untuned four-model average
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
}

SAVE_SPREAD_EXPERIMENT = "ElasticNet"
