from sklearn.calibration import CalibratedClassifierCV
from sklearn.frozen import FrozenEstimator
from sklearn.metrics import log_loss


def calibrate_model(model, X_cal, y_cal, X_test, y_test, method: str = "sigmoid"):
    """Calibrate a fitted model, keeping whichever version has better log loss.

    Args:
        method: "sigmoid" (Platt scaling, robust) or "isotonic" (non-parametric, needs more data)

    Returns the calibrated model if it improves log loss, otherwise the original.
    """
    if method == "none":
        return model

    uncal_proba = model.predict_proba(X_test)
    uncal_ll = log_loss(y_test, uncal_proba)

    calibrated = CalibratedClassifierCV(FrozenEstimator(model), method=method)
    calibrated.fit(X_cal, y_cal)

    cal_proba = calibrated.predict_proba(X_test)
    cal_ll = log_loss(y_test, cal_proba)

    improved = cal_ll < uncal_ll
    diff = abs(cal_ll - uncal_ll)

    print(f"    Uncalibrated log loss: {uncal_ll:.4f}")
    print(f"    Calibrated log loss:   {cal_ll:.4f} ({diff:.4f} {'better' if improved else 'worse'}) [{method}]")
    print(f"    Using {'calibrated' if improved else 'uncalibrated'}")

    return calibrated if improved else model
