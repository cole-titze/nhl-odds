from sklearn.calibration import CalibratedClassifierCV
from sklearn.frozen import FrozenEstimator
from sklearn.metrics import log_loss


def calibrate_model(model, X_cal, y_cal, X_test, y_test, method: str | list[str] = "sigmoid", sample_weight=None):
    """Calibrate a fitted model, keeping whichever version has better log loss.

    Args:
        method: "sigmoid", "isotonic", "none", or a list of methods to try — returns whichever scores best.

    Returns the calibrated model if it improves log loss, otherwise the original.
    """
    if method == "none":
        return model

    methods = method if isinstance(method, list) else [method]

    best_ll = log_loss(y_test, model.predict_proba(X_test))
    best_model = model

    for m in methods:
        calibrated = CalibratedClassifierCV(FrozenEstimator(model), method=m)
        calibrated.fit(X_cal, y_cal, sample_weight=sample_weight)
        ll = log_loss(y_test, calibrated.predict_proba(X_test))
        if ll < best_ll:
            best_ll = ll
            best_model = calibrated

    return best_model
