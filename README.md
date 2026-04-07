# nhl-odds

![Unit Tests](https://github.com/cole-titze/nhl-odds/actions/workflows/unit-tests.yml/badge.svg)
![Format Check](https://github.com/cole-titze/nhl-odds/actions/workflows/format-check.yml/badge.svg)
![Database Container](https://github.com/cole-titze/nhl-odds/actions/workflows/docker-build.yml/badge.svg)
![Web API Container](https://github.com/cole-titze/nhl-odds/actions/workflows/webapi-build.yml/badge.svg)
![Frontend Container](https://github.com/cole-titze/nhl-odds/actions/workflows/frontend-build.yml/badge.svg)
![Entry Container](https://github.com/cole-titze/nhl-odds/actions/workflows/entry-build.yml/badge.svg)
![Predictor Container](https://github.com/cole-titze/nhl-odds/actions/workflows/predictor-build.yml/badge.svg)
![Scheduler Container](https://github.com/cole-titze/nhl-odds/actions/workflows/scheduler-build.yml/badge.svg)
![Security Scan](https://github.com/cole-titze/nhl-odds/actions/workflows/security-scan.yml/badge.svg)

The nhl project. This repo collects nhl data from the nhl api, cleans it, and then runs machine learning models on the data to predict outcomes. There is also a full-stack web app to access the information.

# Install and Run

## Prerequisites

- [Install docker desktop](https://www.docker.com/products/docker-desktop/)
- Install PostgreSQL client tools:

```bash
sudo apt-get update && sudo apt-get install -y postgresql-client
```

- Install dotnet:

```bash
sudo apt-get update && sudo apt-get install -y wget
wget https://packages.microsoft.com/config/debian/$(cat /etc/debian_version | cut -d. -f1)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb && rm packages-microsoft-prod.deb
sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
```

## Setup Database

```bash
docker run --restart=always -e POSTGRES_DB=nhl -e POSTGRES_PASSWORD=<YOUR PASSWORD> -p 5432:5432 --name nhl-postgres -d postgres:17-alpine
```

Connect to PostgreSQL and run the schema script:

```bash
psql -h localhost -U postgres -d nhl -f database/Scripts/CreateTables.sql
```

Or restore from a backup:

```bash
docker exec -i nhl-postgres pg_restore -U postgres -d nhl < nhl.dump
```

### Run Data Models

```
python3 -m venv .venv
source .venv/bin/activate
pip install -r game_predictor/requirements.txt
python -m game_predictor
```

### Backup Database

```bash
docker exec nhl-postgres pg_dump -U postgres -Fc nhl > nhl.dump
```

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
EOF
```

| Variable | Required | Description |
|---|---|---|
| `POSTGRES_PASSWORD` | Yes | Database password for the postgres user |
| `ODDS_API_KEY` | No | The Odds API key for daily odds fetching |
| `API_BACKFILL_KEY` | No | The Odds API key for historical odds backfill |
| `CLOUDFLARE_TUNNEL_TOKEN` | No | Cloudflare Tunnel token for public access (see [Cloudflare Tunnel](#cloudflare-tunnel)) |

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

Jobs can also be triggered manually from the Admin page. The API writes a `requested` status to the database, and the scheduler picks it up within ~60 seconds.

## Database Backup / Restore

```bash
# Backup (runs pg_dump inside the container to avoid client/server version mismatch)
docker exec nhl-odds-database-1 pg_dump -U postgres -Fc nhl > nhl.dump

# Restore
docker exec -i nhl-odds-database-1 pg_restore -U postgres --clean --if-exists -d nhl < nhl.dump
```

### Nightly Backup Cron Job

Automatically back up the database every night, keeping the last 7 days:

```bash
mkdir -p ~/Backups
crontab -e
```

Add this line (runs at 1:00 AM, before the 2 AM auto-update):

```
0 1 * * * docker exec nhl-odds-database-1 pg_dump -U postgres -Fc nhl > ~/Backups/nhl-$(date +\%Y\%m\%d).dump 2>> /var/log/nhl-odds-backup.log && find ~/Backups -name "nhl-*.dump" -mtime +7 -delete
```

## Kubernetes Deployment

For deploying to a Kubernetes cluster, see the [ansible-setup](https://github.com/cole-titze/ansible-setup) repo. The playbook at `raspberry-pi-playbooks/cluster/kubernetes/nhl-odds/nhl-odds.yml` deploys the full stack to a K3s cluster, including:

- PostgreSQL database with a persistent volume
- Web API and frontend (with Traefik ingress at `nhl-odds.kubecluster`)
- CronJobs replacing the scheduler container (NHL data collection at 3 AM, odds fetch + prediction at 6 AM CT)

The following variables must be set in `group_vars` or ansible-vault before running:

| Variable | Description |
|---|---|
| `nhl_odds_postgres_password` | Database password for the postgres user |
| `nhl_odds_odds_api_key` | The Odds API key for daily odds fetching |
| `nhl_odds_api_backfill_key` | The Odds API key for historical odds backfill |

> **Note:** The on-demand job dispatch (Admin page buttons) is not supported in the Kubernetes deployment — jobs run on their cron schedules only.

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
