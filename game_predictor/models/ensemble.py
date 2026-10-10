import numpy as np
from joblib import Parallel, delayed
from sklearn.base import BaseEstimator, ClassifierMixin, RegressorMixin, clone
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import StratifiedKFold
from sklearn.utils._tags import ClassifierTags

from .experiment.folds import fit_model


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


def _single_threaded(estimator):
    """Clone with one thread. joblib caps OpenMP threads in its workers, but LightGBM ignores that and
    would start a thread per core in each of the workers."""
    model = clone(estimator)
    if "n_jobs" in model.get_params():
        model.set_params(n_jobs=1)
    return model


def _fit_and_predict(estimator, X, y, sample_weight, train_idx, test_idx):
    w = sample_weight[train_idx] if sample_weight is not None else None
    model = fit_model(_single_threaded(estimator), X[train_idx], y[train_idx], w)
    return test_idx, model.predict_proba(X[test_idx])[:, 1]


class WeightedStackingClassifier(BaseEstimator, ClassifierMixin):
    """Stacking ensemble that applies sample_weight to every model.

    Equivalent to sklearn's StackingClassifier(cv=5, stack_method="predict_proba",
    final_estimator=LogisticRegression()) for binary targets, except sklearn's
    StackingClassifier.fit drops sample_weight — so season decay never reached the stacked
    models. Here each base model gets the weights when its fit() accepts them (KNN doesn't),
    and so does the logistic-regression meta-model.
    """

    def __init__(self, estimators: list | None = None, cv: int = 5):
        self.estimators = estimators or []
        self.cv = cv

    def __sklearn_tags__(self):
        tags = super().__sklearn_tags__()
        tags.estimator_type = "classifier"
        if tags.classifier_tags is None:
            tags.classifier_tags = ClassifierTags()
        return tags

    def fit(self, X, y, sample_weight=None, fitted_estimators=None):
        """fitted_estimators: the base models already fit on all of X, in estimators order. Saves fitting
        them again when the caller has them anyway."""
        X, y = np.asarray(X), np.asarray(y)
        self.classes_ = np.unique(y)
        folds = list(StratifiedKFold(n_splits=self.cv).split(X, y))

        # Out-of-fold P(class 1) from each base model becomes the meta-model's features. Every
        # (model, fold) pair runs at once here, so each fit gets one thread; the final fits below are
        # only one per model, so they keep their own thread settings.
        jobs = [(i, est, tr, te) for i, (_, est) in enumerate(self.estimators) for tr, te in folds]
        outputs = Parallel(n_jobs=-1)(
            delayed(_fit_and_predict)(est, X, y, sample_weight, tr, te) for _, est, tr, te in jobs
        )
        meta = np.zeros((len(y), len(self.estimators)))
        for (i, *_), (test_idx, proba) in zip(jobs, outputs):
            meta[test_idx, i] = proba

        self.final_estimator_ = LogisticRegression().fit(meta, y, sample_weight=sample_weight)
        if fitted_estimators is not None:
            self.estimators_ = list(fitted_estimators)
        else:
            self.estimators_ = Parallel(n_jobs=-1)(
                delayed(fit_model)(clone(est), X, y, sample_weight) for _, est in self.estimators
            )
        return self

    def _meta_features(self, X):
        return np.column_stack([est.predict_proba(X)[:, 1] for est in self.estimators_])

    def predict_proba(self, X):
        return self.final_estimator_.predict_proba(self._meta_features(X))

    def predict(self, X):
        return self.classes_[np.argmax(self.predict_proba(X), axis=1)]


class RegressionEnsemble(BaseEstimator, RegressorMixin):
    """Average-prediction ensemble for regression models."""

    def __init__(self, models: list | None = None):
        self.models = models or []

    def fit(self, X, y=None):
        return self

    def predict(self, X):
        preds = np.array([m.predict(X) for m in self.models])
        return preds.mean(axis=0)
