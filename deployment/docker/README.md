# Docker Deployment

Deploy the full stack (database, API, frontend) using pre-built images from GitHub Container Registry. No repo clone needed.

## 1. Create a project directory

```bash
mkdir ~/nhl-odds && cd ~/nhl-odds
```

## 2. Download the production compose file

```bash
curl -o docker-compose.yml https://raw.githubusercontent.com/cole-titze/nhl-odds/main/docker-compose.prod.yml
```

## 3. Create a `.env` file

```bash
cat > .env <<'EOF'
POSTGRES_PASSWORD=YourSecurePassword123!
ODDS_API_KEY=your-odds-api-key
API_BACKFILL_KEY=your-backfill-api-key
CLOUDFLARE_TUNNEL_TOKEN=your-tunnel-token
ADMIN_API_KEY=your-random-admin-key
EOF
```

| Variable | Required | Description |
|---|---|---|
| `POSTGRES_PASSWORD` | Yes | Database password for the postgres user |
| `ODDS_API_KEY` | No | The Odds API key for daily odds fetching |
| `API_BACKFILL_KEY` | No | The Odds API key for historical odds backfill |
| `CLOUDFLARE_TUNNEL_TOKEN` | No | Cloudflare Tunnel token for public access (see [Cloudflare Tunnel](#cloudflare-tunnel)) |
| `ADMIN_API_KEY` | Yes, to start jobs on demand | Required in the `X-Admin-Key` header by `POST /api/Admin/Start*` (generate with `openssl rand -hex 32`). If unset, those endpoints return 403 outside Development |
| `THROTTLE_TIME_MS` | No | Delay between NHL API requests (default 250; the API returns 429s without one) |

## 4. Log in to GitHub Container Registry

```bash
echo "YOUR_GITHUB_PAT" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

Needs a [GitHub Personal Access Token](https://github.com/settings/tokens) with `read:packages` scope.

## 5. Start the services

```bash
docker compose pull && docker compose up -d
```

To also start the Cloudflare tunnel (requires `CLOUDFLARE_TUNNEL_TOKEN` in `.env`):

```bash
docker compose --profile tunnel pull && docker compose --profile tunnel up -d
```

The site will be available at `http://<host-ip>:8081`.

| Service | Port | Description |
|---|---|---|
| `database` | 5432 | PostgreSQL — auto-creates schema on first run |
| `webapi` | 8080 (internal) | Lightweight .NET API (Alpine) |
| `frontend` | 8081 | Nginx serving React app, proxies `/api/` to webapi |
| `scheduler` | — | Cron-based job scheduler, triggers entry/predictor containers |
| `entry` | — | One-shot data collection container (NHL API, odds, Kalshi) |
| `predictor` | — | One-shot ML prediction container (Python) |
| `cloudflared` | — | Cloudflare Tunnel (optional, requires `tunnel` profile) |
| `uptime-kuma` | 3001 | Uptime monitoring UI (optional, requires `monitoring` profile) |

## 6. Auto-update nightly

```bash
crontab -e
```

Add this line (runs at 2:00 AM, before the 3 AM data collection):

```
0 2 * * * cd ~/nhl-odds && curl -fsSL -o docker-compose.yml https://raw.githubusercontent.com/cole-titze/nhl-odds/main/docker-compose.prod.yml && export PREDICTOR_CPUS=$(($(nproc)/2)) && docker compose --profile jobs --profile tunnel --profile monitoring pull -q && docker compose --profile tunnel --profile monitoring up -d --remove-orphans >> /var/log/nhl-odds-update.log 2>&1
```

## Scheduled Jobs

The scheduler container runs two daily jobs via cron:

- **3:00 AM CT** — Data collection (`entry` container with `RUN_MODE=NhlAdd`)
- **6:00 AM CT** — Odds fetch pipeline: fetches bookmaker odds, Kalshi odds, then runs the ML predictor (sequential `entry` + `predictor` containers)

Jobs can also be triggered manually (Admin page buttons in development, or `curl -X POST -H "X-Admin-Key: $ADMIN_API_KEY" <api>/api/Admin/Start<Job>`). The API writes a `requested` status to the database, and the scheduler picks it up within ~60 seconds.

## Database Backup / Restore

```bash
# Backup (runs pg_dump inside the container to avoid client/server version mismatch)
docker exec nhl-odds-database-1 pg_dump -U postgres -Fc nhl > nhl.dump

# Restore
docker exec -i nhl-odds-database-1 pg_restore -U postgres --clean --if-exists -d nhl < nhl.dump
```

### Upgrading from PostgreSQL 17

The `database` image is now based on PostgreSQL 18, which can't read a PostgreSQL 17 data directory. The 18 image also stores its data under `/var/lib/postgresql` instead of `/var/lib/postgresql/data`, so the compose file mounts `pg-data` at the new path. To move an existing deployment over, dump before pulling the new compose file and restore afterwards:

```bash
# 1. With the old (17) stack still running, take a dump
docker exec nhl-odds-database-1 pg_dump -U postgres -Fc nhl > nhl-pg17.dump

# 2. Stop the stack and remove the old data volume
docker compose down
docker volume rm nhl-odds_pg-data

# 3. Pull the new compose file and images, start the database (schema is created on first run)
curl -o docker-compose.yml https://raw.githubusercontent.com/cole-titze/nhl-odds/main/docker-compose.prod.yml
docker compose pull && docker compose up -d database

# 4. Restore the dump
docker exec -i nhl-odds-database-1 pg_restore -U postgres --clean --if-exists -d nhl < nhl-pg17.dump
```

### Nightly Backup Cron Job

Automatically back up the database every night, keeping the last 7 days:

```bash
mkdir -p ~/Backups
crontab -e
```

Add this line (runs at 1:00 AM, before the 2 AM auto-update):

```
0 1 * * * /usr/bin/docker exec nhl-odds-database-1 pg_dump -U postgres -Fc nhl > ~/Backups/nhl-$(date +\%Y\%m\%d).dump 2>> ~/Backups/backup.log && find ~/Backups -name "nhl-*.dump" -mtime +7 -delete
```

## CI/CD

Images are automatically built and pushed to GHCR on every push to `main` and rebuilt weekly (Sundays) to pick up base image security updates.

| Image | Workflow | Triggers |
|-------|----------|----------|
| `ghcr.io/cole-titze/nhl-odds/webapi` | `webapi-build.yml` | WebApi, DatabaseAccess, Entities changes |
| `ghcr.io/cole-titze/nhl-odds/frontend` | `frontend-build.yml` | frontend/ changes |
| `ghcr.io/cole-titze/nhl-odds/database` | `docker-build.yml` | database/ changes |
| `ghcr.io/cole-titze/nhl-odds/entry` | `entry-build.yml` | Entry, DataGetter, DatabaseAccess, Entities, Services, BookmakerOddsGetter changes |
| `ghcr.io/cole-titze/nhl-odds/predictor` | `predictor-build.yml` | game_predictor/ changes |
| `ghcr.io/cole-titze/nhl-odds/scheduler` | `scheduler-build.yml` | scheduler/ changes |

A weekly [Trivy](https://github.com/aquasecurity/trivy) security scan (`security-scan.yml`) runs after the rebuilds and fails if any CRITICAL or HIGH vulnerabilities are found.

## Cloudflare Tunnel

To expose the site publicly via [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/):

1. Create a tunnel in the Cloudflare dashboard and configure it to route traffic to `http://frontend:80`
2. Add the tunnel token to your `.env` file as `CLOUDFLARE_TUNNEL_TOKEN`
3. Start (or restart) with the tunnel profile: `docker compose --profile tunnel up -d`

If no token is set, the `cloudflared` container simply won't start.

## Uptime Monitoring

To track availability of the public Cloudflare Tunnel URL, start the monitoring profile:

```bash
docker compose --profile monitoring up -d
```

Then open `http://<host-ip>:3001` to complete the [Uptime Kuma](https://github.com/louislam/uptime-kuma) setup. Add a monitor with type **HTTP(s)**, point it at your Cloudflare Tunnel URL, and set the check interval (default 60s).

Uptime Kuma stores its data in a named Docker volume (`uptime-kuma-data`) separate from the NHL database, so uptime history is preserved across container restarts. The nightly auto-update cron job (step 6) includes the `monitoring` profile and will keep the image up to date.
