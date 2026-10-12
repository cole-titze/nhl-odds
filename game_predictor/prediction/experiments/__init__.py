# Experiments are split by bet type: moneyline.py, spread.py, total.py (over/under). features.py configures the
# features the predictor computes itself.
#
# The experiment dicts load on first use: db.queries imports features.py, and the experiment files import the model
# code, which imports db.queries.
from importlib import import_module

_SOURCES = {
    "EXPERIMENTS": "moneyline",
    "SAVE_EXPERIMENT": "moneyline",
    "SPREAD_EXPERIMENTS": "spread",
    "SAVE_SPREAD_EXPERIMENT": "spread",
    "TOTAL_EXPERIMENTS": "total",
    "SAVE_TOTAL_EXPERIMENT": "total",
}

__all__ = [
    "EXPERIMENTS",
    "REGRESSION_EXPERIMENTS",
    "SAVE_EXPERIMENT",
    "SAVE_SPREAD_EXPERIMENT",
    "SAVE_TOTAL_EXPERIMENT",
    "SPREAD_EXPERIMENTS",
    "TOTAL_EXPERIMENTS",
]


def __getattr__(name: str):
    if name == "REGRESSION_EXPERIMENTS":
        return {**__getattr__("SPREAD_EXPERIMENTS"), **__getattr__("TOTAL_EXPERIMENTS")}
    if name in _SOURCES:
        return getattr(import_module(f".{_SOURCES[name]}", __name__), name)
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")
