import argparse

from .workflow import run

parser = argparse.ArgumentParser(description="NHL Game Predictor")
parser.add_argument(
    "--mode",
    choices=["predict", "backfill", "test"],
    default="predict",
    help="predict: upcoming games. backfill: walk-forward all history. test: experiment with all models.",
)
args = parser.parse_args()
run(mode=args.mode)
