# nhl-odds

![Unit Tests](https://github.com/cole-titze/nhl-odds/actions/workflows/unit-tests.yml/badge.svg)
![Format Check](https://github.com/cole-titze/nhl-odds/actions/workflows/format-check.yml/badge.svg)
![Database Container](https://github.com/cole-titze/nhl-odds/actions/workflows/docker-build.yml/badge.svg)
![Web API Container](https://github.com/cole-titze/nhl-odds/actions/workflows/webapi-build.yml/badge.svg)
![Frontend Container](https://github.com/cole-titze/nhl-odds/actions/workflows/frontend-build.yml/badge.svg)

The nhl project. This repo collects nhl data from the nhl api, cleans it, and then runs machine learning models on the data to predict outcomes. There is also a full-stack web app to access the information.

# Install and Run

## Prerequisites

- [Install docker desktop](https://www.docker.com/products/docker-desktop/)
- [Install dotnet](https://dotnet.microsoft.com/en-us/download)

## Setup Database

```
sudo docker pull mcr.microsoft.com/azure-sql-edge:latest
sudo docker run --restart=always --cap-add SYS_PTRACE -e 'ACCEPT_EULA=1' -e 'MSSQL_SA_PASSWORD=<YOUR PASSWORD>' -p 1433:1433 --name azuresqledge -d mcr.microsoft.com/azure-sql-edge
```

- Connect to sql server and run database scripts (connecting to 'localhost' from mssql extension works well)

1. CreateDatabase.sql
2. CreateTables.sql

- Or restore from a bacpac (requires [sqlpackage](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-download): `dotnet tool install --global Microsoft.SqlPackage`):

```
sqlpackage /Action:Import /TargetServerName:localhost,1433 /TargetDatabaseName:nhl \
  /TargetUser:SA /TargetPassword:'<YOUR PASSWORD>' \
  /SourceFile:./nhl.bacpac /TargetTrustServerCertificate:True
```

### Run Data Models

```
python3 -m venv .venv
source .venv/bin/activate
pip install -r game_predictor/requirements.txt
python -m game_predictor
```

### Backup Database

```
sqlpackage /Action:Export /SourceServerName:localhost,1433 /SourceDatabaseName:nhl \
  /SourceUser:SA /SourcePassword:'<YOUR PASSWORD>' \
  /TargetFile:./nhl.bacpac /SourceTrustServerCertificate:True
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
MSSQL_SA_PASSWORD=YourSecurePassword123!
ODDS_API_KEY=your-odds-api-key
API_BACKFILL_KEY=your-backfill-api-key
EOF
```

| Variable | Required | Description |
|---|---|---|
| `MSSQL_SA_PASSWORD` | Yes | Database password for the SA account |
| `ODDS_API_KEY` | No | The Odds API key for daily odds fetching |
| `API_BACKFILL_KEY` | No | The Odds API key for historical odds backfill |

## 4. Log in to GitHub Container Registry

```bash
echo "YOUR_GITHUB_PAT" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

Needs a [GitHub Personal Access Token](https://github.com/settings/tokens) with `read:packages` scope.

## 5. Start the services

```bash
docker compose pull && docker compose up -d
```

The site will be available at `http://<host-ip>:8081`.

| Service | Port | Description |
|---|---|---|
| `database` | 1433 | Azure SQL Edge — auto-creates schema on first run |
| `webapi` | 8080 (internal) | .NET API + Python predictor + scheduled jobs |
| `frontend` | 8081 | Nginx serving React app, proxies `/api/` to webapi |

## 6. Auto-update nightly

```bash
crontab -e
```

Add this line (runs at 2:00 AM, before the 3 AM data collection):

```
0 2 * * * cd ~/nhl-odds && docker compose pull -q && docker compose up -d --remove-orphans >> /var/log/nhl-odds-update.log 2>&1
```

## Scheduled Jobs

The API container runs two daily jobs automatically:

- **3:00 AM** — Data collection (fetches latest game data from NHL API)
- **6:00 AM** — Odds fetch + prediction (fetches bookmaker odds, then runs the ML predictor)

Jobs can also be triggered manually from the Admin page.

## Database Backup / Restore

Requires [sqlpackage](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-download): `dotnet tool install --global Microsoft.SqlPackage`

```bash
# Backup
sqlpackage /Action:Export /SourceServerName:localhost,1433 /SourceDatabaseName:nhl \
  /SourceUser:SA /SourcePassword:'<PASSWORD>' \
  /TargetFile:./nhl.bacpac /SourceTrustServerCertificate:True

# Restore
sqlpackage /Action:Import /TargetServerName:localhost,1433 /TargetDatabaseName:nhl \
  /TargetUser:SA /TargetPassword:'<PASSWORD>' \
  /SourceFile:./nhl.bacpac /TargetTrustServerCertificate:True
```

## CI/CD

Images are automatically built and pushed to GHCR on every push to `main`:

| Image | Workflow | Triggers |
|-------|----------|----------|
| `ghcr.io/cole-titze/nhl-odds/webapi` | `webapi-build.yml` | WebApi, DatabaseAccess, Entities, WebBusinessLogic changes |
| `ghcr.io/cole-titze/nhl-odds/frontend` | `frontend-build.yml` | frontend/ changes |
| `ghcr.io/cole-titze/nhl-odds/database` | `docker-build.yml` | database/ changes |

## Cloudflare Tunnel

To expose the site publicly, set up a [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/) pointing to `http://localhost:8081`.
