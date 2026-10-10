from scipy.stats import norm


def cover_probability(predicted_value: float, residual_std: float, line: float) -> float:
    """P(actual > line) given predicted mean and residual std.

    For spread: P(home covers) = P(goal_diff > spread_line)
    For total: P(over) = P(total_goals > ou_line)
    """
    if residual_std <= 0:
        return 1.0 if predicted_value > line else 0.0
    return 1.0 - norm.cdf(line, loc=predicted_value, scale=residual_std)


def line_cover_probability(target: str, predicted_value: float, residual_std: float, line: float) -> float:
    """P(home covers) for a spread, P(over) for a total.

    A spread line is the home team's handicap, the opposite sign of the margin it must beat: home -1.5 covers
    when goal_diff - 1.5 > 0, i.e. goal_diff > 1.5.
    """
    threshold = -line if target == "spread" else line
    return cover_probability(predicted_value, residual_std, threshold)
