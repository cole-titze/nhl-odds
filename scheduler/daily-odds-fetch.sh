#!/bin/bash
set -e
. /app/.env

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Starting scheduled odds fetch"
# A failed fetch (logged to ErrorLog) shouldn't stop the other fetch or the prediction
failed=0
docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=NextDayOdds entry || failed=1
echo "$(date -u '+%Y-%m-%d %H:%M:%S') Odds fetch finished"

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Starting scheduled Kalshi fetch"
docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=NextDayKalshi entry || failed=1
echo "$(date -u '+%Y-%m-%d %H:%M:%S') Kalshi fetch finished"

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Starting scheduled prediction"
docker compose -f "$COMPOSE_FILE" run --rm predictor
echo "$(date -u '+%Y-%m-%d %H:%M:%S') Prediction finished"
exit $failed
