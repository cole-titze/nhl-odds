from scipy.stats import norm


def cover_probability(predicted_value: float, residual_std: float, line: float) -> float:
    """P(actual > line) given predicted mean and residual std.

    For spread: P(home covers) = P(goal_diff > spread_line)
    For total: P(over) = P(total_goals > ou_line)
    """
    if residual_std <= 0:
        return 1.0 if predicted_value > line else 0.0
    return 1.0 - norm.cdf(line, loc=predicted_value, scale=residual_std)
