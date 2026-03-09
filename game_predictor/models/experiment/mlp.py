from sklearn.neural_network import MLPClassifier

from .types import ModelConfig


def mlp(
    hidden_layer_sizes: tuple[int, ...] = (64, 32),
    max_iter: int = 500,
    random_state: int = 42,
) -> ModelConfig:
    """Create an MLP model config."""
    return ModelConfig(
        cls=MLPClassifier,
        params={
            "hidden_layer_sizes": hidden_layer_sizes,
            "max_iter": max_iter,
            "early_stopping": True,
            "random_state": random_state,
        },
    )
