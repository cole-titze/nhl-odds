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

- Or restore from a backup:

```
docker cp ./nhl.bak azuresqledge:/var/opt/mssql/backup/nhl.bak
docker exec azuresqledge /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P '<YOUR PASSWORD>' -Q "RESTORE DATABASE [nhl] FROM DISK = N'/var/opt/mssql/backup/nhl.bak' WITH REPLACE"
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
docker exec azuresqledge /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P '<YOUR PASSWORD>' -Q "BACKUP DATABASE [nhl] TO DISK = N'/var/opt/mssql/backup/nhl.bak' WITH FORMAT"
docker cp azuresqledge:/var/opt/mssql/backup/nhl.bak ./nhl.bak
```

# Docker Deployment

Deploy the full stack (database, API, frontend) with Docker Compose. Designed to run on a Raspberry Pi or any ARM64/x86 host.

## Quick Start

```bash
# Copy env template and fill in your values
cp .env.example .env

# Start all services
docker compose up -d
```

The site will be available at `http://<host-ip>:8081`.

## Environment Variables

Set these in `.env`:

| Variable | Required | Description |
|---|---|---|
| `MSSQL_SA_PASSWORD` | Yes | Database password for the SA account |
| `ODDS_API_KEY` | No | The Odds API key for daily odds fetching |
| `API_BACKFILL_KEY` | No | The Odds API key for historical odds backfill |

## Services

| Service | Port | Description |
|---|---|---|
| `database` | 1433 | Azure SQL Edge — auto-creates schema on first run |
| `webapi` | 8080 (internal) | .NET API + Python predictor + scheduled jobs |
| `frontend` | 8081 | Nginx serving React app, proxies `/api/` to webapi |

## Restore a Database Backup

```bash
# Copy backup into the running container
docker cp nhl.bak nhl-odds-database-1:/var/opt/mssql/backup/nhl.bak

# Restore (replace <PASSWORD> with your MSSQL_SA_PASSWORD)
docker exec nhl-odds-database-1 /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U SA -P '<PASSWORD>' \
  -Q "RESTORE DATABASE [nhl] FROM DISK = N'/var/opt/mssql/backup/nhl.bak' WITH REPLACE"
```

## Create a Database Backup

```bash
docker exec nhl-odds-database-1 /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U SA -P '<PASSWORD>' \
  -Q "BACKUP DATABASE [nhl] TO DISK = N'/var/opt/mssql/backup/nhl.bak' WITH FORMAT"

docker cp nhl-odds-database-1:/var/opt/mssql/backup/nhl.bak ./nhl.bak
```

## Scheduled Jobs

The API container runs two daily jobs automatically:

- **3:00 AM** — Data collection (fetches latest game data from NHL API)
- **6:00 AM** — Odds fetch + prediction (fetches bookmaker odds, then runs the ML predictor)

Jobs can also be triggered manually from the Admin page.

## Cloudflare Tunnel

To expose the site publicly, set up a [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/) pointing to `http://localhost:8081`.
