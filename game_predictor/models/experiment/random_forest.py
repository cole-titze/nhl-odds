from __future__ import annotations

from sklearn.ensemble import RandomForestClassifier

from .types import ModelConfig


def random_forest(
    n_estimators: int = 200,
    max_depth: int | None = None,
    min_samples_split: int = 2,
    min_samples_leaf: int = 1,
    random_state: int = 42,
) -> ModelConfig:
    """Create a Random Forest model config."""
    return ModelConfig(
        cls=RandomForestClassifier,
        params={
            "n_estimators": n_estimators,
            "max_depth": max_depth,
            "min_samples_split": min_samples_split,
            "min_samples_leaf": min_samples_leaf,
            "random_state": random_state,
        },
    )
