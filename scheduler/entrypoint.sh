#!/bin/bash
# Dump container environment so cron jobs can access it
env > /app/.env
exec crond -f -l 2
