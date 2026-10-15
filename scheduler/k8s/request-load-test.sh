#!/bin/sh
# Weekly CronJob (nhl-odds-weekly-load-test): requests a load test the same way the admin page does.
# save-load-test.sh compares a Light run with the previous ones and writes slow pages to "ErrorLog".
# Skips the week if another job is running or requested, like the admin page refusing.
PRESET="${1:-light}"

OUT=$(psql "$NHL_DATABASE_URL" -qAt -v ON_ERROR_STOP=1 -v job="load-test-$PRESET" <<'SQL'
INSERT INTO "JobStatus" ("JobName", "Status")
SELECT :'job', 'requested'
WHERE NOT EXISTS (SELECT 1 FROM "JobStatus" WHERE "Status" IN ('running', 'requested'))
ON CONFLICT ("JobName") DO UPDATE SET "Status" = 'requested', "FinishedAt" = NULL, "Error" = NULL
RETURNING 'Requested ' || "JobName";
SQL
) || exit 1
echo "${OUT:-Skipped: another job is running or requested}"
