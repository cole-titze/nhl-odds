from sklearn.linear_model import LogisticRegression as LR

from .types import ModelConfig


def logistic_regression(
    max_iter: int = 1000,
    C: float = 1.0,
    random_state: int = 42,
) -> ModelConfig:
    """Create a Logistic Regression model config."""
    return ModelConfig(
        cls=LR,
        params={
            "max_iter": max_iter,
            "C": C,
            "random_state": random_state,
        },
    )
