#!/bin/bash
set -e

echo "=== Running Data Getter ==="
dotnet Entry.dll

echo "=== Running Game Predictor ==="
/app/.venv/bin/python -m game_predictor
