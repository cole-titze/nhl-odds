#!/bin/bash
set -e

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Starting scheduled odds fetch"
docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=NextDayOdds entry
echo "$(date -u '+%Y-%m-%d %H:%M:%S') Odds fetch finished"

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Starting scheduled Kalshi fetch"
docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=KalshiFetch entry
echo "$(date -u '+%Y-%m-%d %H:%M:%S') Kalshi fetch finished"

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Starting scheduled prediction"
docker compose -f "$COMPOSE_FILE" run --rm predictor
echo "$(date -u '+%Y-%m-%d %H:%M:%S') Prediction finished"
