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
cd frontend && npm run dev    # React Router dev server (Vite, with SSR) on http://localhost:5173
cd frontend && npm run build  # production build to frontend/build/ (client assets + server bundle)
cd frontend && npm start      # production server (server.mjs) on :8080: renders pages, serves assets, proxies /api and /mcp to API_URL
cd frontend && npm run a11y   # WCAG 2.2 AA checks: axe (Chromium) + reflow/focus/scroll checks in Chromium, Firefox, WebKit (mocked API; run build first; A11Y_BROWSERS=chromium to limit; A11Y_PARTS=axe|checks and A11Y_AXE_SHARD=2/4 split it like CI does)

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
   - `RUN_MODE` — `"NhlAdd"`, `"NhlUpdate"`, `"NextDayOdds"`, `"BackfillOdds"`, `"ClosingOdds"`, `"BackfillClosingOdds"`, `"NextDayKalshi"`, `"BackfillKalshi"`, `"CleanAll"`, `"LineupSnapshot"` or `"SearchIndex"`
   - `THROTTLE_TIME_MS` — delay between the game/player/schedule NHL API requests (ms). The NHL publishes no limit (api-web answers 429 at about one request per 500 ms), so every NHL client waits at least 600 ms between requests (`ThrottledHttpClient.NHL_MIN_GAP`; a smaller THROTTLE_TIME_MS is raised to it) and retries 429s with growing waits
   - `BACKFILL_GAME_IDS` — comma-separated game IDs to limit `NhlUpdate` to
   - `REFETCH_DAYS` — `NhlAdd` re-fetches games played in this many past days to pick up NHL stat corrections (default 4 — corrections land within ~2 days; `0` turns it off)
   - `SEASON_START` / `SEASON_END` — first and last season for `NhlUpdate`, as season start years (`2009` is 2009-10; default: every season, 2009 to the current one). Same value in both does one season
   - `FROM_STORED` — `true` makes `NhlUpdate` rebuild from the stored raw responses, never the API
   - `CLEAN_PARALLELISM` — how many seasons the data cleaner cleans at once (default 4; each holds two seasons of data in memory)
   - `ODDS_API_KEY` — The Odds API key (for `NextDayOdds` and `ClosingOdds`; the free key's 500 credits a month reset on the 1st at 00:00 UTC)
   - `API_BACKFILL_KEY` — The Odds API key for backfill (can be different quota)
   - `ELASTICSEARCH_URL` / `ELASTICSEARCH_USERNAME` / `ELASTICSEARCH_PASSWORD` — Elasticsearch for `SearchIndex` (locally also `Elasticsearch:ELASTICSEARCH_URL` etc. in `appsettings.Local.json`)
   - `EMBEDDING_URL` / `EMBEDDING_MODEL` — the Ollama server and model (default `nomic-embed-text:v1.5`; keep a version tag, never `latest`, since a moved tag changes the weights under the same name and the vectors stop comparing) that give `SearchIndex`'s articles their vectors for semantic search (locally also `Embedding:EMBEDDING_URL` etc.). Unset indexes articles for keyword search only

2. **`src/Entry/appsettings.Local.json`** (local dev, gitignored):
   - Copy the structure from `src/Entry/appsettings.json` and fill in the connection string.
   - Read only when `NHL_DATABASE` isn't set. Its values fill in only what the environment didn't set, so `RUN_MODE=... BACKFILL_GAME_IDS=... dotnet run --project src/Entry` works locally.

3. **`src/WebApi/appsettings.Development.json`** (local dev for Web API, gitignored):
   - Needs a `ConnectionStrings:NHL_DATABASE` entry. Falls back to `NHL_DATABASE` env var.
   - `ADMIN_API_KEY` (env var or config) — required in the `X-Admin-Key` header by every `/api/Admin` endpoint (`[RequireAdminKey]` on `AdminController`). If unset, they're allowed in Development and return 403 elsewhere. The Admin page asks for the key and keeps it in localStorage; its nav link only shows in development or once a key is stored.
   - `ELASTICSEARCH_URL` / `ELASTICSEARCH_USERNAME` / `ELASTICSEARCH_PASSWORD` (env var or config) — site search (`SearchController`, `GET /api/Search?q=`, `frontend/src/pages/SearchPage.tsx` and the navbar box). Unset turns it off (`/api/Search` 404s, the page says so); Elasticsearch down answers 503. Results are cached 2 minutes in their own 500-entry cache (`SearchGetter`), apart from `ApiCache`. The `search_requests_total{result}` metric counts outcomes
   - `EMBEDDING_URL` / `EMBEDDING_MODEL` / `EMBEDDING_MIN_SIMILARITY` (env var or config) — semantic article search: each query is embedded by the same model as the articles (`QueryEmbedder`, 2 s timeout) and a knn search runs beside the keyword one. Unset, slow or down: keyword search alone (a failure skips the model for a minute, `EmbeddingCircuit`). `EMBEDDING_MIN_SIMILARITY` (default 0.55, raw cosine) is the least similarity a vector match needs; tune it per model. The `search_embedding_total{result}` metric counts ok/error/skipped
   - `OLLAMA_URL` (env var or config) — experimental chat assistant (`AssistantController`, `frontend/src/components/Assistant.tsx`): the Ollama server it asks (e.g. `http://localhost:11434`). Unset turns it off: `Status` answers `{enabled:false}`, `Chat` 404s and the widget stays hidden. `OLLAMA_MODEL` picks the model (default `qwen3.5:9b`, asked for a 32k context). Before the tool loop, `AssistantGate` makes one short tool-less call that answers ON/OFF and turns off-topic questions (math, homework, code) away with a fixed reply; an unclear verdict, error or timeout lets the question through. `OLLAMA_GATE_MODEL` picks its model (default: `OLLAMA_MODEL`, already loaded). A turned-away question still uses a rate-limit token. Results go to the log and the `assistant_gate_total{result}` metric. Limits are in `AssistantLimits`

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
| `src/Search` | Site search over Elasticsearch, shared by `Entry` (builds the indices) and `WebApi` (queries them): a small REST client (`ElasticsearchClient`, plain HTTP + JSON, no official client), index mappings (`SearchIndexes`), documents (`SearchDocumentMapper`), the query (`SiteSearch`) and article embeddings through Ollama's `/api/embed` (`OllamaEmbedder`, `ArticleEmbeddings`) |
| `tests/` | xUnit/MSTest projects mirroring the `src/` projects under test |
| `frontend` | React 19 + TypeScript + Vite + Tailwind CSS v4 frontend — game odds, team stats, team detail pages. Server-rendered with React Router framework mode (`src/routes.ts`, `src/root.tsx`); Games, Teams, Team and Game pages have route `loader`s for the first load (Teams sends each game once with only the fields it reads; an unknown game is a 404), client navigation fetches in the page |

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

**Run modes** (`ModeType` enum; adding or renaming one also means updating the mode table on the [wiki](https://github.com/cole-titze/nhl-odds/wiki) Home page, repo `git@github.com:cole-titze/nhl-odds.wiki.git`):
- `NhlAdd` — skips seasons/games that already exist in the DB (fast incremental). Re-fetches games played inside `REFETCH_DAYS`, then after cleaning fetches this season's missing rosters (`GameRosterSpot`), shift charts (`GameShift`) and goal replays (`GoalReplay`, `GoalReplayPosition`: player and puck x/y every 0.1 s); `GameDetailFetch` records what's done
- `NhlUpdate` — re-fetches and overwrites games (`BACKFILL_GAME_IDS`, else the `SEASON_START`–`SEASON_END` seasons) and their rosters, shift charts and goal replays, then re-cleans those seasons and the one after. `FROM_STORED=true` rebuilds from `NhlApiResponse` instead of the API (after a parser change); anything not stored keeps its rows
- `NextDayOdds` — fetches upcoming game odds from The Odds API. A failed request goes to `ErrorLog` (`Source` `NextDayOdds`) and fails the job
- `BackfillOdds` — backfills historical odds from The Odds API (rate-limited, uses caching)
- `ClosingOdds` — runs every 5 minutes and saves every Odds API bookmaker's price for games starting in the next 6 minutes to `BookmakerClosingOdds`, `BookmakerClosingSpreads` and `BookmakerClosingTotals` (the live endpoint drops finished games, so closes can't be fetched later on the free key). It reads the credits left (free `/sports` call), holds back 3 per 6 AM `NextDayOdds` run left this month, and fetches all three markets only if every later start time this month could get them too, else drops spread, then total. Failures, including no credits left, go to `ErrorLog` (`Source` `ClosingOdds`) and fail the job
- `BackfillClosingOdds` — same tables from paid historical snapshots (`API_BACKFILL_KEY`, 30 credits per start time): one request 1 minute before each start time since 2020-06-06 with played games and no Odds API close. Snapshots are cached in `BookmakerOddsResponse` by query time, so a stopped run resumes; it stops at the first failed request
- `NextDayKalshi` — fetches Kalshi open market odds for upcoming games. A failed request goes to `ErrorLog` (`Source` `NextDayKalshi`) and fails the job. Then saves the closing moneyline (last one-minute candle before puck drop) for games played in the last 3 days to `BookmakerClosingOdds` (failures: `Source` `NextDayKalshiClosing`)
- `BackfillKalshi` — backfills historical Kalshi odds using candlestick data at 6am CT on game day (no API key needed), then the closing moneyline for every played game missing one. Markets settled before Kalshi's historical cutoff are read from its `/historical` endpoints
- `CleanAll` — skips data collection and re-cleans every game in every season (run after changing how `GameCleaned` features are computed)
- `LineupSnapshot` — saves NHL.com content that only shows its current state, each step independent (failures go to `ErrorLog` and fail the job at the end); runs every 30 min on game days:
  - the lineup projections article (this season's, found by `NhlSeasonArticles`; last season's fails the step from October) → `LineupArticle` when its text has changed, then each new game section → `LineupGame` (game, teams, status report) and `LineupPlayer` (forward lines, defense pairs, goalie order, scratched/injured/suspended, matched to player ids through each team's current NHL roster (exact name, else same last name, first initial and position), else a unique name in `Player`); `LineupArticleParse` marks versions parsed
  - the betting-partner odds widget (DraftKings US, FanDuel CA) → `PartnerOdds`, one row per team per line (`Line`/`Outcome` split from the qualifier), each new version (after puck drop it shows live odds: pre-game lines are `PartnerUpdatedUTC < StartTimeUTC`)
  - the newest 100 NHL.com articles (league and team sites) plus the rewritten-in-place ones (backup goalies list; this and last season's trade and signing trackers and the daily props article, found by `NhlSeasonArticles` so no slug changes each season) → `NhlArticle` (a version per new text), `NhlArticleTag` (team/player/game tags carry the NHL id), `NhlArticleMention` (players linked in the text) and, for the props article, `NhlPropsPick`
  - NHL requests are spaced at least 600 ms apart per host (it answers 429 faster than that)
- `SearchIndex` — rebuilds the site-search indices (`nhl-teams`, `nhl-players`, `nhl-games`, `nhl-articles`) from Postgres: each into a new `{alias}-{timestamp}` index, then the alias moves to it atomically and old ones are deleted, so search never goes down; a failed build leaves the old index. Runs hourly. Failures go to `ErrorLog` (`Source` `SearchIndex`). Needs `ELASTICSEARCH_URL`; production uses a writer limited to `create_index`/`write`/`manage`/`read` on `nhl-*` (the webapi a `read`-only user). With `EMBEDDING_URL`, articles get vectors (`ArticleEmbeddings`): an unchanged article (same `EmbeddingKey`, a hash of the model, its document prefix and the text) reuses the live index's vector, the rest are embedded newest first, 4 per request and one request at a time (searches share the embedding server, which keeps a second slot for their queries), for up to 30 minutes per run; the model failing leaves the rest without vectors and never fails the job. Only one build runs at a time (a Postgres advisory lock taken in `Program.cs`; a run that finds it taken exits without touching `JobStatus`), so the Admin page's "Rebuild Search Index" (`StartSearchIndex`, no once-a-day limit) can't race the hourly CronJob; on the cluster that rebuild is `kubectl create job --from=cronjob/nhl-odds-entry-search-index`, so it runs with the CronJob's image and settings

**Raw NHL responses** — every request to a per-game endpoint (gamecenter boxscore/right-rail/play-by-play, shift charts, goal replays) or a player page goes through `RawResponseCache`, which stores the 200 responses gzipped in `NhlApiResponse` (a new row only when the text changes; `LastSeenUTC` moves otherwise). Its policy is `Fresh` (always the API) or, for `NhlUpdate` with `FROM_STORED=true`, `StoredOnly`. A game's copy is only served if it was fetched 8+ hours after the game started. Store errors are logged and ignored, so the store can never break a run.

**Game ID encoding** (`NhlApiDataGetter.GetGameId`):
```
gameId = (seasonStartYear × 1,000,000) + 20,000 + gameNumber
// e.g. season 2022, game 1 → 2022020001
```
The first 4 digits of a game ID are always the season start year.

**Game event tables** — each play-by-play event type (Goal, Shot, Hit, Penalty, etc.) has its own DB table and `DbModel`. `GameRepository` handles all of them via a typed `_dbSetEventMap` dictionary and `AddOrUpdateEvents<T>`.

**Site search** (`src/Search`) — one `_msearch` over the four aliases per query. A date in the query ("oilers march 3", "10/30", "tonight") is taken out by `SiteSearch.Parse` (no year: the nearest such date) and filters games on their Eastern schedule date; without one, games nearest today rank first. Names match as you type (`search_as_you_type`), accent-folded, with up to 1 typo (2 from 8 letters, `NameFuzziness`) and fan nicknames (`SearchIndexes.TeamNicknames`, a search-time synonym filter, so editing it needs no reindex). Articles are the newest version of each `NhlArticle`, markdown stripped; snippets mark matches with private-use characters (`MATCH_START`/`MATCH_END`) rather than HTML. Article links are `nhl.com/news/{slug}`. With embeddings on, articles are also found by meaning ("who got traded" finds "acquired"): a fifth `_msearch` item is a knn search over the `embedding` field (a `dense_vector`, int8 HNSW, only mapped when some article has a vector), and `SiteSearch.FuseArticles` merges it with the keyword hits by reciprocal rank fusion (Elasticsearch's own `rrf` retriever needs a paid license). Embedding text is the headline, summary and first 2000 characters, with each model's query/document prefix (`EmbeddingPrompts`). A knn search against an index without vectors fails alone and leaves keyword results.

**Commits are explicit** — repositories accumulate EF change tracking; callers must invoke `repo.Commit()` (which calls `SaveChangesAsync()`). `NhlDataManager` commits after each logical save step.

**Predictor features** — `game_predictor` reads the `GameCleaned` columns (`GAME_CLEANED_COLUMNS`) and adds features it computes itself: 16 expected-goals features from the shot events (`game_predictor/features/expected_goals.py`, `XG_FEATURE_COLUMNS`) and a margin-of-victory Elo carried between seasons (`game_predictor/features/elo.py`, `ELO_FEATURE_COLUMNS`; it needs every season in order, which the cleaner never loads). `FEATURE_COLUMNS` is all three lists and feeds the win, spread and total models alike. `game_predictor/prediction/experiments/features.py` is the one place to change the predictor-computed features (xG windows and stats, Elo settings) and to leave any feature column out of every model.

**Schedule features** — `MapScheduleToDbGameCleaned` adds travel and last-season columns to `GameCleaned`: km travelled since the last game and over 7 days, games in the last 7 days, home stand/road trip length, the away team's time-zone shift and puck drop on its body clock, and last regular season's goal difference and points percentage (relocated teams use their predecessor's). Venue coordinates are a city table in that mapper; upcoming games (no venue yet) and cities missing from it count as the home team's city.

**Feature windows** — column names say which games a feature averages over. Each group uses the windows that did best in walk-forward evals (2026-10-09), so they differ on purpose:

| Features | `Recent…` / short | unprefixed / long |
|---|---|---|
| Team box-score stats, save % (`MapGameToDbGameCleaned`, `RosterScorer`) | last 5 games | this season (`…AtHome`/`…AtAway`: home or away games this season and last) |
| Event stats: Corsi, PP/PK, faceoffs, penalties (`EventAggregator`) | last 5 games | this season and last |
| Roster values (`RosterScorer`) | each player's last 5 games | their games this season and last |
| Predictor xG features (`Last10…`, `Last82…`) | last 10 games | last 82 games |

A name without `Recent` or `Last{n}` means the group's long window. A new window size goes in the name (`Last{n}…`).

**Data collection starts at 2009** (first season with modern play-by-play stats), defined as `START_YEAR` in `src/Entry/DataGetterEntry.cs`.
