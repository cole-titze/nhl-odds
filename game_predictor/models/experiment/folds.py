"""Multi-season fold scoring shared by the Optuna tuners.

A fold is (X_train, X_val, y_train, y_val, sample_weight). Tuners score each trial on every
fold and minimize the game-weighted mean, so params are chosen across several seasons
instead of overfitting a single one.
"""

import inspect
from collections.abc import Callable

import numpy as np
from sklearn.metrics import log_loss, mean_absolute_error

Fold = tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray, np.ndarray | None]


def fit_model(model, X, y, sample_weight=None):
    if sample_weight is not None and "sample_weight" in inspect.signature(model.fit).parameters:
        model.fit(X, y, sample_weight=sample_weight)
    else:
        model.fit(X, y)
    return model


def log_loss_score(model, X, y) -> float:
    return log_loss(y, model.predict_proba(X), labels=[0, 1])


def mae_score(model, X, y) -> float:
    return mean_absolute_error(y, model.predict(X))


def mean_fold_score(
    make_model: Callable[[], object],
    folds: list[Fold],
    score: Callable[[object, np.ndarray, np.ndarray], float],
    use_weights: bool = True,
) -> float:
    """Fit a fresh model on each fold and return the game-weighted mean validation score."""
    total, n = 0.0, 0
    for X_train, X_val, y_train, y_val, w in folds:
        model = fit_model(make_model(), X_train, y_train, w if use_weights else None)
        total += score(model, X_val, y_val) * len(y_val)
        n += len(y_val)
    return total / n
