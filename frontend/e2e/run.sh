#!/usr/bin/env bash
# End-to-end smoke test: loads CreateTables.sql and seed.sql into an empty Postgres database,
# starts the real Web API and the production frontend server, and runs smoke.mjs against them.
#
#   E2E_DATABASE="Host=localhost;Username=postgres;Password=...;Database=nhl_e2e" frontend/e2e/run.sh
#
# The database must exist and be empty. Needs psql, the .NET SDK, and `npm ci` in frontend/
# (the build runs here unless E2E_SKIP_BUILD=1). Ports: E2E_API_PORT (5299), E2E_WEB_PORT (4299).
set -euo pipefail

: "${E2E_DATABASE:?set E2E_DATABASE to an Npgsql connection string for an empty database}"
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
API_PORT="${E2E_API_PORT:-5299}"
WEB_PORT="${E2E_WEB_PORT:-4299}"
LOGS="$(mktemp -d)"

# psql takes the same settings as libpq environment variables
conn() { tr ';' '\n' <<<"$E2E_DATABASE" | sed -n "s/^$1=//Ip" | head -1; }
export PGHOST="$(conn Host)" PGPORT="$(conn Port)" PGUSER="$(conn Username)" PGPASSWORD="$(conn Password)" PGDATABASE="$(conn Database)"
[ -n "$PGPORT" ] || unset PGPORT

echo "Loading schema and seed data into $PGDATABASE on $PGHOST"
psql -v ON_ERROR_STOP=1 -q -f "$ROOT/database/Scripts/CreateTables.sql"
psql -v ON_ERROR_STOP=1 -q -f "$ROOT/frontend/e2e/seed.sql"

pids=()
cleanup() {
  for pid in "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done
}
trap cleanup EXIT

wait_for() {
  for _ in $(seq 1 90); do
    if curl -fsS -o /dev/null "$1"; then return 0; fi
    sleep 1
  done
  echo "Timed out waiting for $1" >&2
  return 1
}

echo "Starting the Web API on :$API_PORT"
dotnet build "$ROOT/src/WebApi" -c Release -v quiet -nologo
NHL_DATABASE="$E2E_DATABASE" ASPNETCORE_URLS="http://127.0.0.1:$API_PORT" ASPNETCORE_ENVIRONMENT=Production \
  dotnet run --project "$ROOT/src/WebApi" -c Release --no-build --no-launch-profile >"$LOGS/webapi.log" 2>&1 &
pids+=($!)
# The cache warmer fills the current season's caches at startup; wait for a real answer
wait_for "http://127.0.0.1:$API_PORT/api/GameOdds/GetAnchorDate?seasonStartYear=2000" \
  || { cat "$LOGS/webapi.log"; exit 1; }

cd "$ROOT/frontend"
if [ "${E2E_SKIP_BUILD:-}" != "1" ]; then npm run build; fi
echo "Starting the frontend on :$WEB_PORT"
PORT="$WEB_PORT" API_URL="http://127.0.0.1:$API_PORT" node server.mjs >"$LOGS/frontend.log" 2>&1 &
pids+=($!)
wait_for "http://127.0.0.1:$WEB_PORT/healthz" || { cat "$LOGS/frontend.log"; exit 1; }

status=0
E2E_BASE="http://127.0.0.1:$WEB_PORT" node e2e/smoke.mjs || status=$?
if [ "$status" -ne 0 ]; then
  echo "--- Web API log"; tail -n 80 "$LOGS/webapi.log"
  echo "--- frontend log"; tail -n 80 "$LOGS/frontend.log"
fi
exit "$status"
