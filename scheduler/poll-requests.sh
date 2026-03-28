#!/bin/bash
. /app/.env

# Poll the JobStatus table for rows with status='requested' and kick off the appropriate container.
# Runs every minute via cron.

REQUESTED=$(psql "$NHL_DATABASE_URL" -Atc "SELECT \"JobName\" FROM \"JobStatus\" WHERE \"Status\" = 'requested' LIMIT 1")

if [ -z "$REQUESTED" ]; then
    exit 0
fi

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Picked up requested job: $REQUESTED"

case "$REQUESTED" in
    data-collection)
        docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=NhlAdd entry
        ;;
    odds-fetch)
        docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=NextDayOdds entry
        ;;
    odds-backfill)
        docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=BackfillOdds entry
        ;;
    kalshi-fetch)
        docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=KalshiFetch entry
        ;;
    kalshi-backfill)
        docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=BackfillKalshi entry
        ;;
    prediction)
        docker compose -f "$COMPOSE_FILE" run --rm predictor
        ;;
    prediction-backfill)
        docker compose -f "$COMPOSE_FILE" run --rm -e PREDICTOR_MODE=backfill predictor
        ;;
    *)
        echo "Unknown job: $REQUESTED"
        ;;
esac
