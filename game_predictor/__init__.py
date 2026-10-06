import importlib.util

# PyTorch is only installed via requirements-experimental.txt (for TabM). On macOS it and
# lightgbm/xgboost bundle separate OpenMP runtimes: torch must load first or TabM segfaults,
# and even then lightgbm/xgboost can't train in the same process. So keep the experimental
# deps in their own venv and run TabM-only experiments there.
if importlib.util.find_spec("torch"):
    import torch  # noqa: F401
