import numpy as np
from sklearn.base import BaseEstimator, ClassifierMixin
from sklearn.utils._tags import ClassifierTags


class Ensemble(BaseEstimator, ClassifierMixin):
    """Average-probability ensemble that wraps already-fitted sklearn models.

    Implements the sklearn estimator interface so it works with
    CalibratedClassifierCV and other sklearn utilities.
    """

    def __init__(self, models: list | None = None):
        self.models = models or []

    def __sklearn_tags__(self):
        tags = super().__sklearn_tags__()
        tags.estimator_type = "classifier"
        if tags.classifier_tags is None:
            tags.classifier_tags = ClassifierTags()
        return tags

    def fit(self, X, y=None):
        if y is not None:
            self.classes_ = np.unique(y)
        elif self.models:
            self.classes_ = self.models[0].classes_
        return self

    def predict_proba(self, X):
        probas = np.array([m.predict_proba(X) for m in self.models])
        return probas.mean(axis=0)

    def predict(self, X):
        return np.argmax(self.predict_proba(X), axis=1)
