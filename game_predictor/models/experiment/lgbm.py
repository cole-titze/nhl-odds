from lightgbm import LGBMClassifier

from .types import ModelConfig


def lgbm(
    n_estimators: int = 200,
    learning_rate: float = 0.05,
    max_depth: int = 6,
    random_state: int = 42,
) -> ModelConfig:
    """Create a LightGBM model config."""
    return ModelConfig(
        cls=LGBMClassifier,
        params={
            "n_estimators": n_estimators,
            "learning_rate": learning_rate,
            "max_depth": max_depth,
            "random_state": random_state,
            "verbosity": -1,
        },
    )
