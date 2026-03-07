import argparse

from .prediction.workflow import run

parser = argparse.ArgumentParser(description="NHL Game Predictor")
parser.add_argument(
    "--mode",
    choices=["predict", "backfill"],
    default="predict",
    help="predict: only unplayed games. backfill: all current season games.",
)
args = parser.parse_args()
run(mode=args.mode)
