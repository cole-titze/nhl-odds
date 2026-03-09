from sklearn.decomposition import PCA
from sklearn.feature_selection import SelectKBest, f_classif
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import MinMaxScaler, StandardScaler


def standard_pipeline(k_best: int = 50, pca_components: int = 15) -> Pipeline:
    """Standard preprocessing pipeline used by most experiments."""
    return Pipeline(
        [
            ("scaler", StandardScaler()),
            ("minmax", MinMaxScaler()),
            ("select", SelectKBest(f_classif, k=k_best)),
            ("pca", PCA(n_components=pca_components)),
        ]
    )
