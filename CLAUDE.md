# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A .NET 10 C# solution that collects NHL game data from the NHL public API, stores it in a PostgreSQL database, and is intended to feed data-cleaning and machine learning pipelines for game outcome prediction. Also includes an ASP.NET Core Web API that serves game odds, team stats, and log loss data to a React frontend.

## Commands

```bash
# Build solution
dotnet build

# Run the data collector (from repo root)
dotnet run --project src/Entry

# Run the web API
dotnet run --project src/WebApi

# Run all tests
dotnet test

# Build a specific project
dotnet build src/DataGetter/DataGetter.csproj

# Frontend (from frontend/ directory)
cd frontend && npm install    # install dependencies
cd frontend && npm run dev    # Vite dev server on http://localhost:5173
cd frontend && npm run build  # production build to frontend/dist/
cd frontend && npm run a11y   # WCAG 2.2 AA checks: axe (Chromium) + reflow/focus/scroll checks in Chromium, Firefox, WebKit (mocked API; run build first; A11Y_BROWSERS=chromium to limit)

# Python game predictor (from repo root)
python3 -m venv .venv
source .venv/bin/activate
pip install -r game_predictor/requirements.txt
pip install -r game_predictor/requirements-experimental.txt  # local-only models (CatBoost, TabM); not in the predictor image
python -m game_predictor.libomp  # macOS: make torch share Homebrew's libomp (re-run after torch upgrades)
python -m game_predictor
```

## Configuration

The app reads config in priority order:

1. **Environment variables** (production):
   - `NHL_DATABASE` — full PostgreSQL connection string (e.g. `Host=localhost;Database=nhl;Username=postgres;Password=...`)
   - `RUN_MODE` — `"NhlAdd"`, `"NhlUpdate"`, `"NextDayOdds"`, `"BackfillOdds"`, `"KalshiFetch"`, `"BackfillKalshi"`, `"BackfillGame"`, `"BackfillGoalieStats"`, `"CleanAll"`, or `"LineupSnapshot"`
   - `THROTTLE_TIME_MS` — delay between NHL API requests (ms)
   - `BACKFILL_GAME_IDS` — comma-separated game IDs to re-fetch (for `BackfillGame` mode)
   - `REFETCH_DAYS` — `NhlAdd` re-fetches games played in this many past days to pick up NHL stat corrections (default 7, `0` turns it off)
   - `CLEAN_PARALLELISM` — how many seasons the data cleaner cleans at once (default 4; each holds two seasons of data in memory)
   - `ODDS_API_KEY` — The Odds API key (for `NextDayOdds` mode)
   - `API_BACKFILL_KEY` — The Odds API key for backfill (can be different quota)

2. **`src/Entry/appsettings.Local.json`** (local dev, gitignored):
   - Copy the structure from `src/Entry/appsettings.json` and fill in the connection string.
   - Read only when `NHL_DATABASE` isn't set. Its values fill in only what the environment didn't set, so `RUN_MODE=... BACKFILL_GAME_IDS=... dotnet run --project src/Entry` works locally.

3. **`src/WebApi/appsettings.Development.json`** (local dev for Web API, gitignored):
   - Needs a `ConnectionStrings:NHL_DATABASE` entry. Falls back to `NHL_DATABASE` env var.
   - `ADMIN_API_KEY` (env var or config) — required in the `X-Admin-Key` header by every `/api/Admin` endpoint (`[RequireAdminKey]` on `AdminController`). If unset, they're allowed in Development and return 403 elsewhere. The Admin page asks for the key and keeps it in localStorage; its nav link only shows in development or once a key is stored.

## Database Setup

Requires PostgreSQL running in Docker:

```bash
docker run --restart=always -e POSTGRES_DB=nhl -e POSTGRES_PASSWORD=<YOUR PASSWORD> \
  -p 5432:5432 --name nhl-postgres -d postgres:18-alpine
```

Then run the schema script:

```bash
psql -h localhost -U postgres -d nhl -f database/Scripts/CreateTables.sql
```

## Architecture

### Project Structure

.NET projects live under `src/` (production) and `tests/` (test projects). Polyglot apps stay at the repo root.

| Project | Role |
|---|---|
| `src/Entry` | Entrypoint — wires DI, reads config, kicks off `DataGetterEntry.Main()` |
| `src/DataGetter` | Business logic — orchestrates fetching and saving per-season/game |
| `src/Services` | NHL API HTTP clients — deserializes raw JSON into `ServiceModels` |
| `src/DatabaseAccess` | EF Core repositories — maps domain models to/from DB (includes `Web*Repository` for the web API) |
| `src/Entities` | Shared library — all models (domain, DB, service response, web ViewModels) and mappers |
| `src/DataCleaner` | Stub — future data-cleaning pipeline |
| `database` | SQL scripts for schema (not a C# project) |
| `src/WebApi` | ASP.NET Core Web API — controllers, view model mappers, Swagger |
| `src/BookmakerOddsGetter` | Fetches and backfills odds from The Odds API and Kalshi (no API key needed for Kalshi) |
| `tests/` | xUnit/MSTest projects mirroring the `src/` projects under test |
| `frontend` | React 19 + TypeScript + Vite + Tailwind CSS v4 frontend — game odds, team stats, team detail pages |

### Data Flow

```
NHL API
  → src/Services/NhlData (HTTP + JSON → ServiceModels)
  → src/Entities/ServiceModels/Mappers (ServiceModels → domain Models)
  → src/DataGetter/BusinessLogic (orchestration, caching, deduplication)
  → src/Entities/Mappers (domain Models → DbModels)
  → DatabaseAccess repositories (EF Core upserts → PostgreSQL)
```

### Web API Data Flow

```
HTTP Request
  → src/WebApi/Controllers (ASP.NET Core controllers)
  → src/DatabaseAccess/Web*Repository (EF Core queries → PostgreSQL)
  → src/WebApi/Mappers (domain models → ViewModels)
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
- `src/Entities/ServiceModels/Mappers/` — NHL API response → domain model
- `src/Entities/Mappers/` — domain model ↔ DB model
- `src/DatabaseAccess/Web*Repository/Mappers/` — web DB model → web domain model
- `src/WebApi/Mappers/` — web domain model → view model

**Run modes** (`ModeType` enum):
- `NhlAdd` — skips seasons/games that already exist in the DB (fast incremental)
- `NhlUpdate` — re-fetches and overwrites existing records
- `NextDayOdds` — fetches upcoming game odds from The Odds API + Kalshi open markets
- `BackfillOdds` — backfills historical odds from The Odds API (rate-limited, uses caching)
- `KalshiFetch` — fetches current Kalshi open market odds for upcoming games
- `BackfillKalshi` — backfills historical Kalshi odds using candlestick data at 6am CT on game day (no API key needed)
- `BackfillGame` — force re-fetches the games in `BACKFILL_GAME_IDS` and overwrites them
- `BackfillGoalieStats` — re-fetches only goalies' shorthanded shots/goals (one boxscore request per game) for games saved before the mapper read them; re-runnable, skips games that already have them
- `CleanAll` — skips data collection and re-cleans every game in every season (run after changing how `GameCleaned` features are computed)
- `LineupSnapshot` — saves NHL.com content that only shows its current state, each step independent (failures go to `ErrorLog` and fail the job at the end); runs every 30 min on game days:
  - the lineup projections article → `LineupArticle` when its text has changed
  - the betting-partner odds widget (DraftKings US, FanDuel CA) → `PartnerOdds`, one row per team per line, each new version (after puck drop it shows live odds: pre-game lines are `PartnerUpdatedUTC < StartTimeUTC`)
  - the newest 100 NHL.com articles (league and team sites) plus the rewritten-in-place ones in `NhlArticleManager.RollingSlugs` → `NhlArticle` (a version per new text) and `NhlArticleTag` (team/player/game tags carry the NHL id)

**Game ID encoding** (`NhlApiDataGetter.GetGameId`):
```
gameId = (seasonStartYear × 1,000,000) + 20,000 + gameNumber
// e.g. season 2022, game 1 → 2022020001
```
The first 4 digits of a game ID are always the season start year.

**Game event tables** — each play-by-play event type (Goal, Shot, Hit, Penalty, etc.) has its own DB table and `DbModel`. `GameRepository` handles all of them via a typed `_dbSetEventMap` dictionary and `AddOrUpdateEvents<T>`.

**Commits are explicit** — repositories accumulate EF change tracking; callers must invoke `repo.Commit()` (which calls `SaveChangesAsync()`). `NhlDataManager` commits after each logical save step.

**Predictor features** — `game_predictor` reads the `GameCleaned` columns (`GAME_CLEANED_COLUMNS`) and adds 16 expected-goals features it computes itself from the shot events (`game_predictor/features/expected_goals.py`, `XG_FEATURE_COLUMNS`). `FEATURE_COLUMNS` is both lists and feeds the win, spread and total models alike.

**Feature windows** — column names say which games a feature averages over. Each group uses the windows that did best in walk-forward evals (2026-10-09), so they differ on purpose:

| Features | `Recent…` / short | unprefixed / long |
|---|---|---|
| Team box-score stats, save % (`MapGameToDbGameCleaned`, `RosterScorer`) | last 5 games | this season (`…AtHome`/`…AtAway`: home or away games this season and last) |
| Event stats: Corsi, PP/PK, faceoffs, penalties (`EventAggregator`) | last 5 games | this season and last |
| Roster values (`RosterScorer`) | each player's last 5 games | their games this season and last |
| Predictor xG features (`Last10…`, `Last82…`) | last 10 games | last 82 games |

A name without `Recent` or `Last{n}` means the group's long window. A new window size goes in the name (`Last{n}…`).

**Data collection starts at 2009** (first season with modern play-by-play stats), defined as `START_YEAR` in `src/Entry/DataGetterEntry.cs`.
