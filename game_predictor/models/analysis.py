import numpy as np
from lightgbm import LGBMClassifier
from sklearn.preprocessing import MinMaxScaler, StandardScaler


def run_shap_analysis(X_train, X_test, y_train, y_test, feature_names: list[str]):
    """Train a LightGBM model and compute SHAP feature importance.

    Uses StandardScaler + MinMaxScaler only (no SelectKBest/PCA) so features
    stay interpretable.
    """
    import shap

    # Scale features without dimensionality reduction
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)

    minmax = MinMaxScaler()
    X_train_scaled = minmax.fit_transform(X_train_scaled)
    X_test_scaled = minmax.transform(X_test_scaled)

    # Train LightGBM with default params
    model = LGBMClassifier(n_estimators=200, learning_rate=0.05, max_depth=6, verbosity=-1, random_state=42)
    model.fit(X_train_scaled, y_train)

    # Compute SHAP values
    explainer = shap.TreeExplainer(model)
    shap_values = explainer.shap_values(X_test_scaled)

    # For binary classification, shap_values may be a list of two arrays
    if isinstance(shap_values, list):
        shap_values = shap_values[1]  # class 1 (away win)

    mean_abs_shap = np.mean(np.abs(shap_values), axis=0)
    feature_importance = sorted(zip(feature_names, mean_abs_shap), key=lambda x: x[1], reverse=True)

    print(f"\n{'=' * 60}")
    print("  SHAP Feature Importance (LightGBM)")
    print(f"{'=' * 60}")

    print(f"\n  Top 20 most important features:")
    print(f"  {'Rank':<6} {'Feature':<45} {'Mean |SHAP|':>10}")
    print(f"  {'-' * 63}")
    for i, (name, val) in enumerate(feature_importance[:20], 1):
        print(f"  {i:<6} {name:<45} {val:>10.4f}")

    print(f"\n  Bottom 20 least important features:")
    print(f"  {'Rank':<6} {'Feature':<45} {'Mean |SHAP|':>10}")
    print(f"  {'-' * 63}")
    for i, (name, val) in enumerate(feature_importance[-20:], len(feature_importance) - 19):
        print(f"  {i:<6} {name:<45} {val:>10.4f}")
