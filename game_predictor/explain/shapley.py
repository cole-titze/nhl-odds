"""Shapley values of feature groups, for any model (#155).

The win model is a KNN + logistic regression stack behind scaling, feature selection and PCA, so
tree SHAP doesn't apply. This explains the whole pipeline as one function of the raw features,
with interventional SHAP over groups of columns: a group's value is its average marginal effect on
the predicted probability when its columns switch from background games' values to this game's,
over random orders of the groups (each order and its reverse, which lowers the variance).

Explaining ~15 groups instead of ~180 columns keeps it to (groups + 1) × orders × background
predictions per game, and the values still add up exactly: base + sum of a game's values equals
its prediction.
"""

from collections.abc import Callable, Sequence

import numpy as np


def group_shapley(
    predict: Callable[[np.ndarray], np.ndarray],
    X: np.ndarray,
    background: np.ndarray,
    groups: Sequence[Sequence[int]],
    orders: int = 8,
    seed: int = 0,
    permutations: Sequence[Sequence[int]] | None = None,
) -> tuple[np.ndarray, float]:
    """Returns (values of shape (len(X), len(groups)), base), where base is the mean prediction over
    `background`. `permutations` replaces the random orders (tests pass all of them for exact values)."""
    n_groups = len(groups)
    if permutations is None:
        rng = np.random.default_rng(seed)
        permutations = []
        for _ in range(max(orders // 2, 1)):
            p = rng.permutation(n_groups)
            permutations += [p, p[::-1]]
    perms = [np.asarray(p) for p in permutations]
    cols = [np.asarray(g, dtype=int) for g in groups]
    m, n_features = background.shape
    base = float(np.mean(predict(background)))

    values = np.zeros((len(X), n_groups))
    for i, x in enumerate(X):
        # Every step of every order for this game, predicted in one call
        rows = np.empty((len(perms), n_groups + 1, m, n_features))
        for k, p in enumerate(perms):
            z = background.copy()
            rows[k, 0] = z
            for j, g in enumerate(p):
                z[:, cols[g]] = x[cols[g]]
                rows[k, j + 1] = z
        means = predict(rows.reshape(-1, n_features)).reshape(len(perms), n_groups + 1, m).mean(axis=2)
        for k, p in enumerate(perms):
            values[i, p] += np.diff(means[k])
        values[i] /= len(perms)
    return values, base
