from sklearn.neighbors import KNeighborsClassifier

from .types import ModelConfig


def knn(
    n_neighbors: int = 5,
    weights: str = "uniform",
    metric: str = "minkowski",
) -> ModelConfig:
    """Create a K-Nearest Neighbors model config."""
    return ModelConfig(
        cls=KNeighborsClassifier,
        params={
            "n_neighbors": n_neighbors,
            "weights": weights,
            "metric": metric,
        },
    )
