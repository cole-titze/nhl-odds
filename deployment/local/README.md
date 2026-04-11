# Local Development

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

## Run Data Models

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r game_predictor/requirements.txt
python -m game_predictor
```

## Backup Database

```bash
docker exec nhl-postgres pg_dump -U postgres -Fc nhl > nhl.dump
```
