#!/bin/sh
# Check for jobs requested from the admin API once a minute.
# A plain loop instead of crond: busybox crond needs root, and the loop keeps the container env.
while :; do
    /app/poll-requests.sh
    sleep 60
done
