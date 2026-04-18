#!/bin/sh
env > /app/.env
exec crond -f -l 2
