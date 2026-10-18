#!/bin/bash
# Starts an empty database (schema only), the webapi and the frontend from the given
# images and checks the API, SSR pages and the frontend's /api proxy answer 200.
# Used as the promotion gate in the image workflows; runs the same on an arm64 Mac.
#
#   smoke-test.sh <webapi image> <frontend image>
set -euo pipefail

webapi=${1:?webapi image}
frontend=${2:?frontend image}
net=nhl-smoke-$$
pw=smoke
here=$(cd "$(dirname "$0")/../.." && pwd)

cleanup() {
  if [ "${failed:-1}" = 1 ]; then
    for c in webapi frontend; do echo "--- $c logs"; docker logs "$net-$c" 2>&1 | tail -40 || true; done
  fi
  docker rm -f "$net-db" "$net-webapi" "$net-frontend" >/dev/null 2>&1 || true
  docker network rm "$net" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker network create "$net" >/dev/null
docker run -d --name "$net-db" --network "$net" --network-alias database \
  -e POSTGRES_DB=nhl -e POSTGRES_PASSWORD=$pw \
  -v "$here/database/Scripts/CreateTables.sql:/docker-entrypoint-initdb.d/01-create-tables.sql:ro" \
  postgres:18-alpine >/dev/null
for i in $(seq 60); do
  # initdb restarts the server once, so wait for the schema rather than the first accept
  docker exec "$net-db" psql -U postgres -d nhl -tAc 'SELECT 1 FROM "GameCleaned" LIMIT 1' >/dev/null 2>&1 && break
  [ "$i" = 60 ] && { echo "database never came up"; exit 1; }
  sleep 2
done

docker run -d --name "$net-webapi" --network "$net" --network-alias webapi \
  -e NHL_DATABASE="Host=database;Database=nhl;Username=postgres;Password=$pw" "$webapi" >/dev/null
docker run -d --name "$net-frontend" --network "$net" -p 127.0.0.1::8080 "$frontend" >/dev/null
port=$(docker port "$net-frontend" 8080/tcp | head -1 | sed 's/.*://')

# Webapi has no published port: reach it through the frontend's /api proxy and from a curl container
api() { docker run --rm --network "$net" curlimages/curl:8.17.0 -fsS -o /dev/null -w '%{http_code}' --max-time 20 "http://webapi:8080$1"; }

for i in $(seq 60); do
  [ "$(api /api/Team/GetAllTeams?seasonStartYear=2026 2>/dev/null)" = 200 ] && break
  [ "$i" = 60 ] && { echo "webapi never answered"; exit 1; }
  sleep 2
done

check() {
  local code
  code=$(curl -sS -o /dev/null -w '%{http_code}' --max-time 30 "http://127.0.0.1:$port$1" || true)
  echo "$code $1"
  [ "$code" = 200 ]
}

ok=1
for path in /api/Team/GetAllTeams?seasonStartYear=2026 /api/GameOdds/GetAnchorDate?seasonStartYear=2026 \
    "/api/GameOdds/GetGameOddsInDateRange?startDate=2026-10-01&endDate=2026-10-31&seasonStartYear=2026"; do
  code=$(api "$path" || true); echo "$code webapi $path"; [ "$code" = 200 ] || ok=0
done
for path in /healthz / /teams /api/Team/GetAllTeams?seasonStartYear=2026; do
  check "$path" || ok=0
done
[ $ok = 1 ] || { echo "smoke test FAILED"; exit 1; }
failed=0
echo "smoke test passed"
