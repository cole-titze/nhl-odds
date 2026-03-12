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
    """A complete experiment: preprocessing pipeline, models, and optional ensemble.

    calibration controls post-training probability calibration:
      "none"     — no calibration
      "sigmoid"  — Platt scaling (2 params, robust with small data)
      "isotonic" — non-parametric (needs more data, can overfit)

    tune controls whether Optuna hyperparameter tuning runs for this experiment
    during backfill. tune_trials sets how many Optuna trials to run.
    """

    models: dict[str, ModelConfig]
    pipeline: Pipeline
    ensemble: list[str] | None = None
    calibration: str = "sigmoid"
    tune: bool = False
    tune_trials: int = 100
