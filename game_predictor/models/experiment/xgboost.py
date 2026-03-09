from xgboost import XGBClassifier

from .types import ModelConfig


def xgboost(
    n_estimators: int = 200,
    learning_rate: float = 0.05,
    max_depth: int = 6,
    random_state: int = 42,
) -> ModelConfig:
    """Create an XGBoost model config."""
    return ModelConfig(
        cls=XGBClassifier,
        params={
            "n_estimators": n_estimators,
            "learning_rate": learning_rate,
            "max_depth": max_depth,
            "random_state": random_state,
            "verbosity": 0,
            "use_label_encoder": False,
        },
    )
