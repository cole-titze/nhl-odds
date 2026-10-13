#!/bin/sh
# Runs after k6 in a load test Job (see poll-requests.sh): saves the run to "LoadTestRun" and
# marks the job completed, or failed with k6's exit code if it didn't finish.
JOB_NAME="$1"

RC=$(cat /out/rc 2>/dev/null || echo "missing")

# k6 exits 99 when a threshold fails; the script sets none, so anything but 0 means it broke
if [ "$RC" != 0 ] || [ ! -f /out/result.env ]; then
    echo "k6 exited with code $RC"
    psql "$NHL_DATABASE_URL" -q -v ON_ERROR_STOP=1 -v job="$JOB_NAME" -v error="k6 exited with code $RC; see the Job's pod logs." <<'SQL'
UPDATE "JobStatus" SET "Status" = 'failed', "FinishedAt" = now() AT TIME ZONE 'UTC', "Error" = :'error'
WHERE "JobName" = :'job';
SQL
    exit 1
fi

. /out/result.env
cat /out/result.env

psql "$NHL_DATABASE_URL" -q -v ON_ERROR_STOP=1 -v job="$JOB_NAME" \
    -v preset="$PRESET" -v target="$TARGET" -v max_vus="$MAX_VUS" \
    -v ramp_up="$RAMP_UP_SECONDS" -v hold="$HOLD_SECONDS" -v ramp_down="$RAMP_DOWN_SECONDS" \
    -v think_min="$THINK_TIME_MIN_SECONDS" -v think_max="$THINK_TIME_MAX_SECONDS" -v page_mix="$PAGE_MIX" \
    -v started="$STARTED_UTC" -v finished="$FINISHED_UTC" \
    -v requests="$REQUESTS" -v rps="$REQUESTS_PER_SECOND" \
    -v p50="$P50_MS" -v p95="$P95_MS" -v p99="$P99_MS" -v max_ms="$MAX_MS" \
    -v http_5xx="$HTTP_5XX" -v http_429="$HTTP_429" -v other_errors="$OTHER_ERRORS" <<'SQL'
BEGIN;
INSERT INTO "LoadTestRun" ("Preset", "Target", "MaxVus", "RampUpSeconds", "HoldSeconds", "RampDownSeconds",
    "ThinkTimeMinSeconds", "ThinkTimeMaxSeconds", "PageMix", "StartedUTC", "FinishedUTC", "Requests",
    "RequestsPerSecond", "P50Ms", "P95Ms", "P99Ms", "MaxMs", "Http5xx", "Http429", "OtherErrors")
VALUES (:'preset', :'target', :'max_vus', :'ramp_up', :'hold', :'ramp_down',
    :'think_min', :'think_max', :'page_mix',
    (:'started'::timestamptz AT TIME ZONE 'UTC'), (:'finished'::timestamptz AT TIME ZONE 'UTC'), :'requests',
    :'rps', :'p50', :'p95', :'p99', :'max_ms', :'http_5xx', :'http_429', :'other_errors');
UPDATE "JobStatus" SET "Status" = 'completed', "FinishedAt" = now() AT TIME ZONE 'UTC', "Error" = NULL
WHERE "JobName" = :'job';
COMMIT;
SQL
