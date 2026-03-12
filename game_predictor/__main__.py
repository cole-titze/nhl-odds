import argparse

from .workflow import run

parser = argparse.ArgumentParser(description="NHL Game Predictor")
parser.add_argument(
    "--mode",
    choices=["predict", "backfill"],
    default="predict",
    help="predict: train and predict unplayed games (Docker). backfill: experiment with all models, save test set.",
)
parser.add_argument(
    "--shap",
    action="store_true",
    help="Run SHAP feature importance analysis (backfill mode only).",
)
args = parser.parse_args()
run(mode=args.mode, shap=args.shap)
