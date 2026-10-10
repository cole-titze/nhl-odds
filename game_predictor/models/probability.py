import math

from scipy.stats import norm


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
    """P(home covers) for a spread, P(over) for a total. A push counts as not covering.

    A spread line is the home team's handicap, the opposite sign of the margin it must beat: home -1.5 covers
    when goal_diff - 1.5 > 0, i.e. goal_diff > 1.5.
    """
    threshold = -line if target == "spread" else line
    return cover_probability(predicted_value, residual_std, threshold)
