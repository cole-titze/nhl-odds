from __future__ import annotations

from dataclasses import dataclass, field

from sklearn.base import ClassifierMixin, RegressorMixin
from sklearn.pipeline import Pipeline


@dataclass
class ModelConfig:
    """Configuration for a single sklearn/LightGBM model."""

    cls: type[ClassifierMixin | RegressorMixin]
    params: dict = field(default_factory=dict)

    def build(self) -> ClassifierMixin | RegressorMixin:
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

    decay controls exponential recency weighting: weight = exp(-decay * seasons_ago),
    normalized to mean=1. decay=0.0 disables weighting. Try 0.2-0.5 to start.
    """

    models: dict[str, ModelConfig]
    pipeline: Pipeline
    ensemble: list[str] | None = None
    stack: bool = False
    calibration: str = "sigmoid"
    tune: bool = False
    tune_trials: int = 100
    decay: float = 0.0


@dataclass
class RegressionExperiment:
    """A regression experiment for spread/total prediction.

    target controls which value to predict:
      "spread" — HomeGoals - AwayGoals (positive = home won by N)
      "total"  — HomeGoals + AwayGoals

    No calibration (classification concept). Evaluation uses MAE/RMSE.
    """

    models: dict[str, ModelConfig]
    pipeline: Pipeline
    target: str
    ensemble: list[str] | None = None
    decay: float = 0.0
    tune: bool = False
    tune_trials: int = 100
