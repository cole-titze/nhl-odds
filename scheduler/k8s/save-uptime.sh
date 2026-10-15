#!/bin/sh
# CronJob (nhl-odds-uptime-rollup, every 15 minutes): rolls the blackbox probe of the public site up
# into one "UptimeDay" row per Central Time day, today (so far) and the days before it that Prometheus
# still has (it keeps 15). Rows are only replaced by counts with at least as many probes, so a
# Prometheus that lost history can't wipe out a day already saved. A day with no probes gets no row.
set -eu

PROM="${PROMETHEUS_URL:-http://cluster-monitoring-kube-pr-prometheus.default:9090}"
TARGET="${UPTIME_TARGET:-https://nhl.half-built.com/}"
INTERVAL="${PROBE_INTERVAL_SECONDS:-60}"
DAYS_BACK="${UPTIME_DAYS_BACK:-14}"

# Prints the first value of an instant query at a Unix time, or nothing if it has no result
query() {
    curl -sSf --max-time 30 "$PROM/api/v1/query" \
        --data-urlencode "query=$1" --data-urlencode "time=$2" \
        | jq -er '.data.result[0].value[1] // ""'
}

# Each day, when it ends (the next Central midnight, or now for today) and how many seconds of it
# that covers. Postgres does the time zone math, so the 23- and 25-hour DST days come out right.
DAYS=$(psql "$NHL_DATABASE_URL" -qAt -F ' ' -v ON_ERROR_STOP=1 -v days_back="$DAYS_BACK" <<'SQL'
SELECT d::date,
    extract(epoch FROM least(now(), (d + interval '1 day') AT TIME ZONE 'America/Chicago'))::bigint,
    extract(epoch FROM least(now(), (d + interval '1 day') AT TIME ZONE 'America/Chicago')
        - (d AT TIME ZONE 'America/Chicago'))::bigint
FROM generate_series(((now() AT TIME ZONE 'America/Chicago')::date - :days_back)::timestamp,
    (now() AT TIME ZONE 'America/Chicago')::date::timestamp, interval '1 day') d
ORDER BY d;
SQL
)

ROWS=""
while read -r DAY END SPAN; do
    [ "$SPAN" -gt 0 ] || continue
    SELECTOR="{instance=\"$TARGET\"}[${SPAN}s]"
    PROBES=$(query "count_over_time(probe_success$SELECTOR)" "$END")
    [ -n "$PROBES" ] || { echo "$DAY: no probes"; continue; }
    SUCCESSFUL=$(query "sum_over_time(probe_success$SELECTOR)" "$END")
    AVG_MS=$(query "avg_over_time(probe_duration_seconds$SELECTOR) * 1000" "$END")
    EXPECTED=$((SPAN / INTERVAL))
    echo "$DAY: $SUCCESSFUL/$PROBES probes up ($EXPECTED expected), ${AVG_MS:-?} ms average"
    ROWS="$ROWS${ROWS:+,}('$DAY', $EXPECTED, round($PROBES), round($SUCCESSFUL), ${AVG_MS:-NULL}, now() AT TIME ZONE 'UTC')"
done <<EOF
$DAYS
EOF

[ -n "$ROWS" ] || { echo "No probe data for $TARGET"; exit 0; }

psql "$NHL_DATABASE_URL" -q -v ON_ERROR_STOP=1 <<SQL
INSERT INTO "UptimeDay" ("Day", "ExpectedProbes", "Probes", "SuccessfulProbes", "AvgResponseMs", "UpdatedUTC")
VALUES $ROWS
ON CONFLICT ("Day") DO UPDATE SET
    "ExpectedProbes" = EXCLUDED."ExpectedProbes",
    "Probes" = EXCLUDED."Probes",
    "SuccessfulProbes" = EXCLUDED."SuccessfulProbes",
    "AvgResponseMs" = EXCLUDED."AvgResponseMs",
    "UpdatedUTC" = EXCLUDED."UpdatedUTC"
WHERE EXCLUDED."Probes" >= "UptimeDay"."Probes";
SQL
