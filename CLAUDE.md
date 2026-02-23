# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A .NET 9 C# solution that collects NHL game data from the NHL public API, stores it in an Azure SQL Edge database, and is intended to feed data-cleaning and machine learning pipelines for game outcome prediction.

## Commands

```bash
# Build solution
dotnet build

# Run the data collector (from repo root)
dotnet run --project Entry

# Build a specific project
dotnet build DataGetter/DataGetter.csproj
```

There are no automated tests in the solution currently.

## Configuration

The app reads config in priority order:

1. **Environment variables** (production):
   - `NHL_DATABASE` — full SQL Server connection string
   - `RUN_MODE` — `"Add"` or `"Update"`
   - `THROTTLE_TIME_MS` — delay between NHL API requests (ms)

2. **`Entry/appsettings.Local.json`** (local dev, gitignored):
   - Copy the structure from `Entry/appsettings.json` and fill in the connection string.

## Database Setup

Requires Azure SQL Edge running in Docker:

```bash
docker run --restart=always --cap-add SYS_PTRACE \
  -e 'ACCEPT_EULA=1' -e 'MSSQL_SA_PASSWORD=<YOUR PASSWORD>' \
  -p 1433:1433 --name azuresqledge -d mcr.microsoft.com/azure-sql-edge
```

Then connect and run the scripts in order:
1. `database/Scripts/CreateDatabase.sql`
2. `database/Scripts/CreateTables.sql`

## Architecture

### Project Structure

| Project | Role |
|---|---|
| `Entry` | Entrypoint — wires DI, reads config, kicks off `DataGetterEntry.Main()` |
| `DataGetter` | Business logic — orchestrates fetching and saving per-season/game |
| `Services` | NHL API HTTP clients — deserializes raw JSON into `ServiceModels` |
| `DatabaseAccess` | EF Core repositories — maps domain models to/from DB |
| `Entities` | Shared library — all models (domain, DB, service response) and mappers |
| `DataCleaner` | Stub — future data-cleaning pipeline |
| `database` | SQL scripts for schema (not a C# project) |

### Data Flow

```
NHL API
  → Services/NhlData (HTTP + JSON → ServiceModels)
  → Entities/ServiceModels/Mappers (ServiceModels → domain Models)
  → DataGetter/BusinessLogic (orchestration, caching, deduplication)
  → Entities/Mappers (domain Models → DbModels)
  → DatabaseAccess repositories (EF Core upserts → SQL Server)
```

### Key Patterns

**Three model layers in `Entities`:**
- `Models/` — clean domain objects used by business logic
- `DbModels/` — EF Core entities (prefixed `Db`); implement `ICloneableType` for `IsEquivalentTo()` / `Clone()` used during upserts
- `ServiceModels/` — JSON deserialization shapes matching the NHL API responses

**Mapper classes** (never AutoMapper) live in:
- `Entities/ServiceModels/Mappers/` — NHL API response → domain model
- `Entities/Mappers/` — domain model ↔ DB model

**Run modes** (`ModeType` enum):
- `Add` — skips seasons/games that already exist in the DB (fast incremental)
- `Update` — re-fetches and overwrites existing records

**Game ID encoding** (`NhlApiDataGetter.GetGameId`):
```
gameId = (seasonStartYear × 1,000,000) + 20,000 + gameNumber
// e.g. season 2022, game 1 → 2022020001
```
The first 4 digits of a game ID are always the season start year.

**Game event tables** — each play-by-play event type (Goal, Shot, Hit, Penalty, etc.) has its own DB table and `DbModel`. `GameRepository` handles all of them via a typed `_dbSetEventMap` dictionary and `AddOrUpdateEvents<T>`.

**Commits are explicit** — repositories accumulate EF change tracking; callers must invoke `repo.Commit()` (which calls `SaveChangesAsync()`). `NhlDataManager` commits after each logical save step.

**Data collection starts at 2009** (first season with modern play-by-play stats), defined as `START_YEAR` in `Entry/DataGetterEntry.cs`.
