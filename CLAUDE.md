# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A .NET 9 C# solution that collects NHL game data from the NHL public API, stores it in an Azure SQL Edge database, and is intended to feed data-cleaning and machine learning pipelines for game outcome prediction. Also includes an ASP.NET Core Web API that serves game odds, team stats, and log loss data to a React frontend.

## Commands

```bash
# Build solution
dotnet build

# Run the data collector (from repo root)
dotnet run --project Entry

# Run the web API
dotnet run --project WebApi

# Run all tests
dotnet test

# Build a specific project
dotnet build DataGetter/DataGetter.csproj

# Frontend (from frontend/ directory)
cd frontend && npm install    # install dependencies
cd frontend && npm run dev    # Vite dev server on http://localhost:5173
cd frontend && npm run build  # production build to frontend/dist/
```

## Configuration

The app reads config in priority order:

1. **Environment variables** (production):
   - `NHL_DATABASE` — full SQL Server connection string
   - `RUN_MODE` — `"Add"` or `"Update"`
   - `THROTTLE_TIME_MS` — delay between NHL API requests (ms)

2. **`Entry/appsettings.Local.json`** (local dev, gitignored):
   - Copy the structure from `Entry/appsettings.json` and fill in the connection string.

3. **`WebApi/appsettings.Development.json`** (local dev for Web API, gitignored):
   - Needs a `ConnectionStrings:NHL_DATABASE` entry. Falls back to `NHL_DATABASE` env var.

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
| `DatabaseAccess` | EF Core repositories — maps domain models to/from DB (includes `Web*Repository` for the web API) |
| `Entities` | Shared library — all models (domain, DB, service response, web ViewModels) and mappers |
| `DataCleaner` | Stub — future data-cleaning pipeline |
| `database` | SQL scripts for schema (not a C# project) |
| `WebApi` | ASP.NET Core Web API — controllers, view model mappers, Swagger |
| `WebBusinessLogic` | Web API business logic — team stats, game odds, log loss orchestration |
| `frontend` | React 19 + TypeScript + Vite + Tailwind CSS v4 frontend — game odds, team stats, team detail pages |

### Data Flow

```
NHL API
  → Services/NhlData (HTTP + JSON → ServiceModels)
  → Entities/ServiceModels/Mappers (ServiceModels → domain Models)
  → DataGetter/BusinessLogic (orchestration, caching, deduplication)
  → Entities/Mappers (domain Models → DbModels)
  → DatabaseAccess repositories (EF Core upserts → SQL Server)
```

### Web API Data Flow

```
HTTP Request
  → WebApi/Controllers (ASP.NET Core controllers)
  → WebBusinessLogic (orchestration, stats aggregation)
  → DatabaseAccess/Web*Repository (EF Core queries → SQL Server)
  → WebApi/Mappers (domain models → ViewModels)
  → JSON Response
```

### Key Patterns

**Three model layers in `Entities`:**
- `Models/` — clean domain objects used by business logic
- `DbModels/` — EF Core entities (prefixed `Db`); implement `ICloneableType` for `IsEquivalentTo()` / `Clone()` used during upserts
- `ServiceModels/` — JSON deserialization shapes matching the NHL API responses

**Web-specific types in `Entities`** (different schemas, camelCase properties, under `*.Web` sub-namespaces):
- `Models/Web/` (`Entities.Models.Web`) — domain objects (Game, Team, GameOdds, TeamStats)
- `DbModels/Web/` (`Entities.DbModels.Web`) — EF Core entities (DbGame, DbTeam, DbGameOdds, DbLogLoss)
- `ViewModels/` (`Entities.ViewModels`) — API response shapes (TeamVM, GameOddsVM, LogLossVM, etc.)
- `Types/` (`Entities.Types`) — value types (DateRange)

**Web-specific repositories in `DatabaseAccess`:**
- `WebGameOddsRepository/` — game odds queries + mappers
- `WebTeamRepository/` — team queries + mappers
- `WebLogLossRepository/` — log loss queries
- `WebDbContext.cs` (`GameDbContext`) — EF Core DbContext for web-facing tables

**Mapper classes** (never AutoMapper) live in:
- `Entities/ServiceModels/Mappers/` — NHL API response → domain model
- `Entities/Mappers/` — domain model ↔ DB model
- `DatabaseAccess/Web*Repository/Mappers/` — web DB model → web domain model
- `WebApi/Mappers/` — web domain model → view model

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
