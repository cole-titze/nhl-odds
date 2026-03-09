from __future__ import annotations

from dataclasses import dataclass, field

from sklearn.base import ClassifierMixin
from sklearn.pipeline import Pipeline


@dataclass
class ModelConfig:
    """Configuration for a single sklearn/LightGBM model."""

    cls: type[ClassifierMixin]
    params: dict = field(default_factory=dict)

    def build(self) -> ClassifierMixin:
        return self.cls(**self.params)


@dataclass
class Experiment:
    """A complete experiment: preprocessing pipeline, models, and optional ensemble."""

    models: dict[str, ModelConfig]
    pipeline: Pipeline
    ensemble: list[str] | None = None
