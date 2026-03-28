#!/bin/bash
set -e

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Starting scheduled data collection"
docker compose -f "$COMPOSE_FILE" run --rm -e RUN_MODE=NhlAdd entry
echo "$(date -u '+%Y-%m-%d %H:%M:%S') Data collection finished"
