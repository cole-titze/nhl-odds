import numpy as np
from sklearn.base import BaseEstimator, ClassifierMixin, clone
from sklearn.pipeline import FunctionTransformer, Pipeline
from sklearn.utils._tags import ClassifierTags

from ...db.queries import FEATURE_COLUMNS
from .folds import fit_model
from .pipeline import standard_pipeline
from .types import ModelConfig

SEGMENTS = ("b2b", "lopsided")


def _segment_mask(X, segment: str, threshold: float | None = None) -> np.ndarray:
    """Games in the segment, from raw (unscaled) feature columns. Only pre-game info."""
    col = FEATURE_COLUMNS.index
    if segment == "b2b":
        return (X[:, col("HomeIsBackToBack")] > 0) | (X[:, col("AwayIsBackToBack")] > 0)
    if segment == "lopsided":
        return np.abs(X[:, col("HomeGoalDiffAvg")] - X[:, col("AwayGoalDiffAvg")]) >= threshold
    raise ValueError(f"unknown segment {segment!r}, expected one of {SEGMENTS}")


class SegmentRouter(BaseEstimator, ClassifierMixin):
    """Fixed-routing mixture of experts (issue #55).

    Fits `model` twice, each with its own standard_pipeline: a global copy on every game and an expert
    copy on the segment's games only. Games in the segment get blend * expert + (1 - blend) * global;
    the rest get the global model. Takes raw features in FEATURE_COLUMNS order, so the experiment's
    pipeline must pass them through untouched (raw_pipeline()).

    Segments: "b2b" (either team on a back-to-back), "lopsided" (|goal-diff avg gap| in the top third
    of the training games).
    """

    def __init__(self, model=None, segment: str = "b2b", blend: float = 0.25, k_best: int = 99, pca_components=90):
        self.model = model
        self.segment = segment
        self.blend = blend
        self.k_best = k_best
        self.pca_components = pca_components

    def __sklearn_tags__(self):
        tags = super().__sklearn_tags__()
        tags.estimator_type = "classifier"
        if tags.classifier_tags is None:
            tags.classifier_tags = ClassifierTags()
        return tags

    def _fit_expert(self, X, y, sample_weight):
        pipeline = standard_pipeline(k_best=self.k_best, pca_components=self.pca_components)
        X_t = pipeline.fit_transform(X, y)
        return pipeline, fit_model(clone(self.model), X_t, y, sample_weight)

    def fit(self, X, y, sample_weight=None):
        X, y = np.asarray(X, dtype=float), np.asarray(y)
        self.classes_ = np.unique(y)
        self.threshold_ = None
        if self.segment == "lopsided":
            gap = np.abs(
                X[:, FEATURE_COLUMNS.index("HomeGoalDiffAvg")] - X[:, FEATURE_COLUMNS.index("AwayGoalDiffAvg")]
            )
            self.threshold_ = np.quantile(gap, 2 / 3)
        mask = _segment_mask(X, self.segment, self.threshold_)
        w = sample_weight[mask] if sample_weight is not None else None
        self.global_ = self._fit_expert(X, y, sample_weight)
        self.expert_ = self._fit_expert(X[mask], y[mask], w)
        return self

    def predict_proba(self, X):
        X = np.asarray(X, dtype=float)
        pipeline, model = self.global_
        proba = model.predict_proba(pipeline.transform(X))
        mask = _segment_mask(X, self.segment, self.threshold_)
        if mask.any():
            pipeline, model = self.expert_
            expert = model.predict_proba(pipeline.transform(X[mask]))
            proba[mask] = self.blend * expert + (1 - self.blend) * proba[mask]
        return proba

    def predict(self, X):
        return self.classes_[np.argmax(self.predict_proba(X), axis=1)]


def raw_pipeline() -> Pipeline:
    """Pass-through pipeline for SegmentRouter, which runs its own standard_pipeline per expert."""
    return Pipeline([("raw", FunctionTransformer())])


def segment_router(
    models: dict[str, ModelConfig],
    segment: str = "b2b",
    blend: float = 0.25,
    k_best: int = 99,
    pca_components: int | None = 90,
) -> ModelConfig:
    """Create a SegmentRouter config. Several models are stacked (WeightedStackingClassifier), like Default."""
    from ..ensemble import WeightedStackingClassifier

    if len(models) == 1:
        model = next(iter(models.values())).build()
    else:
        model = WeightedStackingClassifier(estimators=[(name, cfg.build()) for name, cfg in models.items()])
    return ModelConfig(
        cls=SegmentRouter,
        params={
            "model": model,
            "segment": segment,
            "blend": blend,
            "k_best": k_best,
            "pca_components": pca_components,
        },
    )
