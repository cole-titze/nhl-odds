# ============================================================================
# EDIT THIS FILE to configure over/under (total goals) models (use RegressionExperiment).
#
# These share the moneyline model's features; only the models/pipeline differ.
# SAVE_TOTAL_EXPERIMENT controls which experiment is saved to DB; set it to None to disable.
# ============================================================================

from ...models.experiment import (
    RegressionExperiment,
    knn_regressor,
    lgbm_regressor,
    mlp_regressor,
    random_forest_regressor,
    standard_pipeline,
    xgboost_regressor,
)

TOTAL_EXPERIMENTS: dict[str, RegressionExperiment] = {
    # Tuned KNN + MLP averaged on MLP's tuned pipeline. Walk-forward MAE 2023-26: 1.865 vs 1.891 for the old
    # four-model ensemble (4/4 seasons better) and 1.873 for always guessing the league average. Ties untuned
    # TabM (1.863) without PyTorch. Neither model nor any other beat the books' line in a betting check.
    "KNN + MLP": RegressionExperiment(
        models={
            "KNN": knn_regressor(n_neighbors=399, weights="distance", metric="minkowski", p=3),
            "MLP": mlp_regressor(
                hidden_layer_sizes=(210, 171, 207),
                max_iter=466,
                learning_rate_init=0.00249335,
                alpha=0.00428878,
                activation="tanh",
            ),
        },
        pipeline=standard_pipeline(k_best=38, pca_components=10, regression=True),
        ensemble=["KNN", "MLP"],
        target="total",
        decay=0.1051,
    ),
    # Previous default: untuned four-model average
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

SAVE_TOTAL_EXPERIMENT = "KNN + MLP"
