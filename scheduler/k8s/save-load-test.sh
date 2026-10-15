#!/bin/sh
# Runs after k6 in a load test Job (see poll-requests.sh): saves the run to "LoadTestRun" and its
# per-page results to "LoadTestRunPage", and marks the job completed, or failed with k6's exit
# code if it didn't finish. Failures, and slow pages or errors in a Light run, go to "ErrorLog"
# (Source 'LoadTest').
JOB_NAME="$1"

RC=$(cat /out/rc 2>/dev/null || echo "missing")

# k6 exits 99 when a threshold fails; the script sets none, so anything but 0 means it broke
if [ "$RC" != 0 ] || [ ! -f /out/result.env ] || [ ! -f /out/pages.sql ]; then
    echo "k6 exited with code $RC"
    psql "$NHL_DATABASE_URL" -q -v ON_ERROR_STOP=1 -v job="$JOB_NAME" -v error="k6 exited with code $RC; see the Job's pod logs." <<'SQL'
BEGIN;
UPDATE "JobStatus" SET "Status" = 'failed', "FinishedAt" = now() AT TIME ZONE 'UTC', "Error" = :'error'
WHERE "JobName" = :'job';
INSERT INTO "ErrorLog" ("TimestampUTC", "ExceptionType", "Message", "StackTrace", "Source")
VALUES (now() AT TIME ZONE 'UTC', 'LoadTestFailed', :'job' || ': ' || :'error', '', 'LoadTest');
COMMIT;
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
    :'rps', :'p50', :'p95', :'p99', :'max_ms', :'http_5xx', :'http_429', :'other_errors')
RETURNING "Id" AS run_id \gset
\i /out/pages.sql
-- Light is the weekly baseline (request-load-test.sh). A page is flagged when its p95 is over 1.5x
-- (and 50 ms more than) the median of its last 4 Light runs; run-to-run noise is about +-20%.
INSERT INTO "ErrorLog" ("TimestampUTC", "ExceptionType", "Message", "StackTrace", "Source")
SELECT now() AT TIME ZONE 'UTC', 'PerformanceRegression',
    format('Light load test run %s: %s p95 %s ms vs %s ms (median of the previous %s runs)',
        :run_id, cur."Page", round(cur."P95Ms"::numeric), round(base.median::numeric), base.runs),
    '', 'LoadTest'
FROM "LoadTestRunPage" cur
JOIN (
    SELECT "Page", percentile_cont(0.5) WITHIN GROUP (ORDER BY "P95Ms") AS median, count(*) AS runs
    FROM (
        SELECT p."Page", p."P95Ms",
            row_number() OVER (PARTITION BY p."Page" ORDER BY p."LoadTestRunId" DESC) AS recent
        FROM "LoadTestRunPage" p
        JOIN "LoadTestRun" r ON r."Id" = p."LoadTestRunId"
        WHERE r."Preset" = 'light' AND r."Id" < :run_id
    ) previous
    WHERE recent <= 4
    GROUP BY "Page"
    HAVING count(*) >= 3
) base ON base."Page" = cur."Page"
WHERE cur."LoadTestRunId" = :run_id AND :'preset' = 'light'
    AND cur."P95Ms" > base.median * 1.5 AND cur."P95Ms" > base.median + 50;
-- Any failed request at Light's load is a bug, not the site running out of room
INSERT INTO "ErrorLog" ("TimestampUTC", "ExceptionType", "Message", "StackTrace", "Source")
SELECT now() AT TIME ZONE 'UTC', 'LoadTestErrors',
    format('Light load test run %s: %s 5xx, %s 429, %s other (timeouts or failed connections)',
        :run_id, :'http_5xx', :'http_429', :'other_errors'),
    '', 'LoadTest'
WHERE :'preset' = 'light' AND :'http_5xx'::int + :'http_429'::int + :'other_errors'::int > 0;
UPDATE "JobStatus" SET "Status" = 'completed', "FinishedAt" = now() AT TIME ZONE 'UTC', "Error" = NULL
WHERE "JobName" = :'job';
COMMIT;
SQL
