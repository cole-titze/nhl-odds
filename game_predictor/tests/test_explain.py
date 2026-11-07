"""Tests for the prediction factors (#155). Run: python -m unittest discover -s game_predictor/tests -t ."""

import itertools
import math
import unittest

import numpy as np
import pandas as pd
from sklearn.decomposition import PCA
from sklearn.linear_model import LogisticRegression
from sklearn.neighbors import KNeighborsClassifier
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

from game_predictor.db.queries import FEATURE_COLUMNS
from game_predictor.explain import background_sample, explain_games
from game_predictor.explain.factors import FACTOR_GROUPS, FEATURE_GROUPS, describe, group_columns
from game_predictor.explain.shapley import group_shapley


def _toy_model(n_features=6, seed=0):
    rng = np.random.default_rng(seed)
    X = rng.normal(size=(400, n_features))
    y = (X[:, 0] + 0.5 * X[:, 1] * X[:, 2] + rng.normal(scale=0.5, size=400) > 0).astype(int)
    model = LogisticRegression().fit(X, y)
    return X, (lambda Z: model.predict_proba(Z)[:, 0])


class GroupShapleyTests(unittest.TestCase):
    def test_all_orders_match_exact_shapley(self):
        """With every order of single-column groups it is exact interventional SHAP."""
        X, predict = _toy_model(n_features=4)
        background, x = X[:20], X[100:103]
        groups = [[i] for i in range(4)]
        values, base = group_shapley(
            predict, x, background, groups, permutations=list(itertools.permutations(range(4)))
        )

        # Brute force over coalitions: phi_i = sum |S|!(n-|S|-1)!/n! (v(S+i) - v(S))
        def v(xi, subset):
            z = background.copy()
            z[:, list(subset)] = xi[list(subset)]
            return predict(z).mean()

        n = 4
        for xi, row in zip(x, values):
            for i in range(n):
                others = [j for j in range(n) if j != i]
                phi = 0.0
                for size in range(n):
                    for s in itertools.combinations(others, size):
                        w = math.factorial(size) * math.factorial(n - size - 1) / math.factorial(n)
                        phi += w * (v(xi, s + (i,)) - v(xi, s))
                self.assertAlmostEqual(row[i], phi, places=10)
        self.assertAlmostEqual(base, predict(background).mean())

    def test_matches_the_shap_library(self):
        try:
            import shap
        except ImportError:
            self.skipTest("shap not installed")
        X, predict = _toy_model(n_features=4)
        background, x = X[:20], X[100:103]
        values, _ = group_shapley(
            predict, x, background, [[i] for i in range(4)], permutations=list(itertools.permutations(range(4)))
        )
        expected = shap.explainers.Exact(predict, shap.maskers.Independent(background, max_samples=20))(x).values
        np.testing.assert_allclose(values, expected, atol=1e-10)

    def test_values_add_up_to_the_prediction(self):
        X, predict = _toy_model(n_features=6)
        background, x = X[:30], X[200:210]
        values, base = group_shapley(predict, x, background, [[0, 1], [2], [3, 4, 5]], orders=4)
        np.testing.assert_allclose(base + values.sum(axis=1), predict(x), atol=1e-12)


class FactorGroupTests(unittest.TestCase):
    def test_every_feature_has_a_group(self):
        grouped = sorted(i for cols in FEATURE_GROUPS.values() for i in cols)
        self.assertEqual(grouped, list(range(len(FEATURE_COLUMNS))))

    def test_unknown_feature_is_rejected(self):
        with self.assertRaises(ValueError):
            group_columns(["SomethingNew"])

    def test_every_group_describes_a_game(self):
        row = {col: 0.5 for col in FEATURE_COLUMNS}
        for group in FEATURE_GROUPS:
            self.assertTrue(describe(group, row), group)

    def test_group_names_are_unique(self):
        names = [g.name for g in FACTOR_GROUPS]
        self.assertEqual(len(names), len(set(names)))

    def test_rest_detail(self):
        row = {col: 0.0 for col in FEATURE_COLUMNS}
        row |= {"HomeHoursSinceLastGame": 48, "AwayHoursSinceLastGame": 24, "AwayIsBackToBack": 1}
        self.assertEqual(describe("Rest", row), "Home 2 days since last game, away back-to-back")


class ExplainGamesTests(unittest.TestCase):
    def test_ranked_rows_add_up_for_a_stacked_pipeline(self):
        """End to end on a pipeline shaped like the win model's (scaling, PCA, KNN + LR)."""
        rng = np.random.default_rng(1)
        n = 300
        df = pd.DataFrame(rng.normal(size=(n, len(FEATURE_COLUMNS))), columns=FEATURE_COLUMNS)
        df["SeasonStartYear"] = rng.choice([2024, 2025, 2026], size=n)
        df["GameId"] = np.arange(n) + 2026020001
        winner = (df["EloAdvantage"] + rng.normal(scale=1, size=n) < 0).astype(int)

        pipeline = Pipeline([("scale", StandardScaler()), ("pca", PCA(n_components=10))])
        Xt = pipeline.fit_transform(df[FEATURE_COLUMNS].to_numpy(), winner)

        class Stack:
            def __init__(self):
                self.lr = LogisticRegression().fit(Xt, winner)
                self.knn = KNeighborsClassifier(n_neighbors=25).fit(Xt, winner)

            def predict_proba(self, Z):
                return (self.lr.predict_proba(Z) + self.knn.predict_proba(Z)) / 2

        model = Stack()
        games = df.iloc[:3]
        rows = explain_games(games, pipeline, model, background_sample(df))

        self.assertEqual(len(rows), 3 * len(FEATURE_GROUPS))
        predicted = model.predict_proba(pipeline.transform(games[FEATURE_COLUMNS].to_numpy()))[:, 0]
        for game_id, p in zip(games["GameId"], predicted):
            mine = [r for r in rows if r["GameId"] == game_id]
            self.assertEqual([r["Rank"] for r in mine], list(range(1, len(FEATURE_GROUPS) + 1)))
            sizes = [abs(r["Contribution"]) for r in mine]
            self.assertEqual(sizes, sorted(sizes, reverse=True))
            self.assertAlmostEqual(mine[0]["BaseHomeOdds"] + sum(r["Contribution"] for r in mine), p, places=10)


if __name__ == "__main__":
    unittest.main()
