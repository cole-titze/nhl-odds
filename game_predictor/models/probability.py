import math
from dataclasses import dataclass

import numpy as np
from scipy.stats import norm
from sklearn.linear_model import LogisticRegression

# Fewer past games with a line than this and cover probabilities are left uncalibrated
MIN_CALIBRATION_GAMES = 500
_P_CLIP = 1e-4


def cover_probability(predicted_value: float, residual_std: float, line: float) -> float:
    """P(actual > line) given predicted mean and residual std, treating actual as a whole number of goals.

    Winning needs at least floor(line) + 1 goals, so the normal is cut at floor(line) + 0.5: a line of 5.5 is
    unchanged, while a whole-number line like 6 no longer counts the push (exactly 6) as half a win.
    """
    threshold = math.floor(line) + 0.5
    if residual_std <= 0:
        return 1.0 if predicted_value > threshold else 0.0
    return 1.0 - norm.cdf(threshold, loc=predicted_value, scale=residual_std)


def line_cover_probability(target: str, predicted_value: float, residual_std: float, line: float) -> float:
    """Uncalibrated P(home covers) for a spread, P(over) for a total. A push counts as not covering.

    A spread line is the home team's handicap, the opposite sign of the margin it must beat: home -1.5 covers
    when goal_diff - 1.5 > 0, i.e. goal_diff > 1.5.
    """
    threshold = -line if target == "spread" else line
    return cover_probability(predicted_value, residual_std, threshold)


def covered(target: str, home_goals: int, away_goals: int, line: float) -> bool:
    """Whether the home team covered the spread line, or the game went over the total line."""
    if target == "spread":
        return home_goals - away_goals + line > 0
    return home_goals + away_goals > line


def _logit(p):
    p = np.clip(p, _P_CLIP, 1 - _P_CLIP)
    return np.log(p / (1 - p))


@dataclass(frozen=True)
class CoverCalibration:
    """Platt scaling: logit(calibrated) = intercept + slope * logit(raw). The default leaves probabilities as they are.

    The raw normal probabilities are too confident against the line (walk-forward 2021-26: spread slope ~0.7,
    total ~0.3), so a slope under 1 pulls them toward 50%.
    """

    intercept: float = 0.0
    slope: float = 1.0

    def apply(self, raw: float) -> float:
        if self == CoverCalibration():
            return raw
        return float(1.0 / (1.0 + np.exp(-(self.intercept + self.slope * _logit(raw)))))


def fit_cover_calibration(raw_probs, outcomes) -> CoverCalibration:
    """Fits Platt scaling on past games' out-of-sample raw cover probabilities and whether they covered."""
    if len(raw_probs) < MIN_CALIBRATION_GAMES:
        return CoverCalibration()
    x = _logit(np.asarray(raw_probs, dtype=float)).reshape(-1, 1)
    model = LogisticRegression(C=1e6).fit(x, np.asarray(outcomes, dtype=int))
    return CoverCalibration(float(model.intercept_[0]), float(model.coef_[0][0]))
