// k6 load test the admin page starts (scheduler/k8s/poll-requests.sh runs it as a Job on node01).
// Each virtual user (VU) browses the site like a person: load a page, wait a few seconds, load another.
// PRESET picks the size; the preset's inputs are saved with the results in "LoadTestRun".
import http from "k6/http";
import { sleep } from "k6";
import { Counter } from "k6/metrics";

const PRESETS = {
  light: {
    maxVus: 25,
    rampUpSeconds: 30,
    holdSeconds: 120,
    rampDownSeconds: 30,
  },
  heavy: {
    maxVus: 150,
    rampUpSeconds: 60,
    holdSeconds: 180,
    rampDownSeconds: 30,
  },
  stress: {
    maxVus: 300,
    rampUpSeconds: 90,
    holdSeconds: 180,
    rampDownSeconds: 30,
  },
  max: {
    maxVus: 1200,
    rampUpSeconds: 180,
    holdSeconds: 180,
    rampDownSeconds: 30,
  },
};
const THINK_TIME_MIN_SECONDS = 1;
const THINK_TIME_MAX_SECONDS = 3;

const PRESET_NAME = __ENV.PRESET || "light";
const PRESET = PRESETS[PRESET_NAME];
if (!PRESET) throw new Error(`Unknown preset '${PRESET_NAME}'`);

// The frontend service: pages are server-rendered there and /api is proxied to the web API,
// so this covers both without going out through Cloudflare
const TARGET = __ENV.TARGET || "http://nhl-odds-frontend";

// Last season's regular-season games all exist (1,312 a season since 2021-22)
const now = new Date();
const currentSeason =
  now >= new Date(now.getUTCFullYear(), 8, 20)
    ? now.getUTCFullYear()
    : now.getUTCFullYear() - 1;
const LAST_SEASON_GAMES = 1312;

// What a visit does, and how often: most actions load one page (server-rendered), some make the API
// calls a browser makes after a page loads or while someone types in the search box. Each request is
// counted under its own name (the per-page rows in "LoadTestRunPage").
const SEARCH_TERMS = [
  "McDavid",
  "Matthews",
  "Crosby",
  "MacKinnon",
  "Makar",
  "Kucherov",
  "Ovechkin",
  "Hughes",
  "Draisaitl",
  "Pastrnak",
  "Shesterkin",
  "Hellebuyck",
  "Bedard",
  "Celebrini",
  "Maple Leafs",
  "Oilers",
  "Bruins",
  "Rangers",
  "Avalanche",
  "Golden Knights",
  "Panthers",
  "Lightning",
  "Canadiens",
  "Kraken",
  "injury update",
  "trade rumors",
  "starting goalie",
  "power play",
  "overtime winner",
  "rookie of the year",
  "salary cap",
  "mcdavd",
];
const OG_PAGES = ["games", "teams", "strategies", "leaders", "about", "site"];
// The Leaders page's stats (frontend/src/utils/leaders.ts)
const LEADER_STATS = [
  "points",
  "goals",
  "assists",
  "shots",
  "plusMinus",
  "powerPlayGoals",
  "penaltyMinutes",
  "hits",
  "blockedShots",
  "timeOnIcePerGame",
  "savePercentage",
  "goalsAgainstAverage",
  "saves",
  "shutouts",
  "gamesStarted",
];
const OG_GAMES = 20;
const firstGameId = (currentSeason - 1) * 1000000 + 20001;

const ACTIONS = [
  { label: "/", weight: 25, run: () => get("/", "/") },
  {
    // The game page renders with its factors and players; the browser then asks for the
    // lineup and odds movement
    label: "/game/:id + lineup + odds movement",
    weight: 16,
    run: () => {
      const gameId = randomGameId();
      get("/game/:id", `/game/${gameId}`);
      get("/api/Lineup", `/api/Lineup/GetGameLineup?gameId=${gameId}`);
      get(
        "/api/OddsMovement",
        `/api/OddsMovement/GetOddsMovement?gameId=${gameId}`,
      );
    },
  },
  {
    label: "/team/:id",
    weight: 9,
    run: (d) => get("/team/:id", `/team/${pick(d.teamIds)}`),
  },
  { label: "/teams", weight: 6, run: () => get("/teams", "/teams") },
  {
    label: "/strategies",
    weight: 5,
    run: () => get("/strategies", "/strategies"),
  },
  { label: "/about", weight: 3, run: () => get("/about", "/about") },
  {
    label: "/player/:id",
    weight: 8,
    run: (d) => get("/player/:id", `/player/${pick(d.playerIds)}`),
  },
  {
    label: "/track-record",
    weight: 4,
    run: () => get("/track-record", "/track-record"),
  },
  {
    // The Leaders page (#182), server-rendered for a stat from the URL (shared links)
    label: "/leaders",
    weight: 4,
    run: () =>
      get(
        "/leaders",
        `/leaders${Math.random() < 0.5 ? "" : `?stat=${pick(LEADER_STATS)}`}`,
      ),
  },
  {
    // Picking another stat or season on the page: the browser asks the API for that list
    label: "Leaders stat switch",
    weight: 3,
    run: () =>
      get(
        "/api/Leaders",
        `/api/Leaders/GetStatLeaders?stat=${pick(LEADER_STATS)}&seasonStartYear=${currentSeason - (Math.random() < 0.8 ? 0 : 1)}`,
      ),
  },
  {
    // A shared or bookmarked search link: the search runs in the server render
    label: "/search?q=",
    weight: 6,
    run: () =>
      get("/search?q=", `/search?q=${encodeURIComponent(pick(SEARCH_TERMS))}`),
  },
  { label: "search box typing + search", weight: 8, run: typeAndSearch },
  {
    label: "calendar .ics",
    weight: 1,
    run: (d) =>
      get("/api/Calendar .ics", `/api/Calendar/Team/${pick(d.teamIds)}.ics`),
  },
  {
    // Link previews (chat apps and social sites fetch these). Shared links bunch up on a few
    // games, so the game images come from a small set, like real traffic (each image is
    // rendered once, then served from the frontend's cache)
    label: "/og/*.png",
    weight: 2,
    run: (d) => {
      const roll = Math.random();
      const path =
        roll < 0.5
          ? `/og/game/${firstGameId + Math.floor(Math.random() * OG_GAMES)}.png`
          : roll < 0.8
            ? `/og/team/${pick(d.teamIds)}.png`
            : `/og/${pick(OG_PAGES)}.png`;
      get("/og/*.png", path);
    },
  },
];
const TOTAL_WEIGHT = ACTIONS.reduce((sum, a) => sum + a.weight, 0);

// Every request name above, for the per-name stats
const REQUEST_NAMES = [
  "/",
  "/game/:id",
  "/api/Lineup",
  "/api/OddsMovement",
  "/team/:id",
  "/teams",
  "/strategies",
  "/about",
  "/player/:id",
  "/track-record",
  "/leaders",
  "/api/Leaders",
  "/search?q=",
  "/api/Search/Suggest",
  "/api/Search",
  "/api/Calendar .ics",
  "/og/*.png",
];

// The search box asks for suggestions once typing pauses for 200 ms (SUGGEST_DELAY_MS in
// SearchBox.tsx); pressing Enter goes to the search page, which fetches the full results
const SUGGEST_DELAY_MS = 200;
function typeAndSearch() {
  const term = pick(SEARCH_TERMS);
  for (let length = 1; length <= term.length; length++) {
    const pauseMs = 80 + Math.random() * 220;
    const typed = term.slice(0, length).trim();
    if (
      typed.length >= 2 &&
      (pauseMs > SUGGEST_DELAY_MS || length === term.length)
    ) {
      get(
        "/api/Search/Suggest",
        `/api/Search/Suggest?q=${encodeURIComponent(typed)}`,
      );
    }
    sleep(pauseMs / 1000);
  }
  get("/api/Search", `/api/Search?q=${encodeURIComponent(term)}`);
}

const http5xx = new Counter("http_5xx");
const http429 = new Counter("http_429");
const otherErrors = new Counter("other_errors");

// Stats are kept for all requests (kind:page, which leaves out setup's sitemap request) and
// for each name; k6 only reports a tag's numbers when a threshold names it, so these always pass
const SUBMETRIC_FILTERS = [
  "kind:page",
  ...REQUEST_NAMES.map((name) => `name:${name}`),
];
const thresholds = {};
for (const filter of SUBMETRIC_FILTERS) {
  thresholds[`http_req_duration{${filter}}`] = ["max>=0"];
  thresholds[`http_reqs{${filter}}`] = ["count>=0"];
  for (const counter of ["http_5xx", "http_429", "other_errors"]) {
    thresholds[`${counter}{${filter}}`] = ["count>=0"];
  }
}

export const options = {
  thresholds,
  stages: [
    { duration: `${PRESET.rampUpSeconds}s`, target: PRESET.maxVus },
    { duration: `${PRESET.holdSeconds}s`, target: PRESET.maxVus },
    { duration: `${PRESET.rampDownSeconds}s`, target: 0 },
  ],
  discardResponseBodies: true,
  summaryTrendStats: ["med", "p(95)", "p(99)", "max"],
  setupTimeout: "60s",
};

function pick(list) {
  return list[Math.floor(Math.random() * list.length)];
}

function randomGameId() {
  return (
    (currentSeason - 1) * 1000000 +
    20000 +
    1 +
    Math.floor(Math.random() * LAST_SEASON_GAMES)
  );
}

function pickAction() {
  let roll = Math.random() * TOTAL_WEIGHT;
  for (const action of ACTIONS) {
    roll -= action.weight;
    if (roll < 0) return action;
  }
  return ACTIONS[0];
}

// One request, counted under its name, with its errors counted by kind
function get(name, path) {
  const tags = { name, kind: "page" };
  const res = http.get(`${TARGET}${path}`, {
    // The web API rate-limits per CF-Connecting-IP, so each VU gets its own address
    // (198.18.0.0/15 is reserved for benchmarking) and is limited like one real visitor
    // Browsers ask for compressed pages; without this k6 downloads them uncompressed (~6x the bytes)
    headers: {
      "CF-Connecting-IP": `198.18.${Math.floor(__VU / 256)}.${__VU % 256}`,
      "Accept-Encoding": "gzip, br",
    },
    tags,
    timeout: "30s",
  });
  if (res.status >= 500) http5xx.add(1, tags);
  else if (res.status === 429) http429.add(1, tags);
  else if (res.status === 0 || res.status >= 400) otherErrors.add(1, tags);
}

// The team and player ids come from the sitemap, which lists every team and player page
export function setup() {
  const res = http.get(`${TARGET}/sitemap.xml`, {
    responseType: "text",
    timeout: "30s",
  });
  const teamIds = [...String(res.body || "").matchAll(/\/team\/(\d+)</g)].map(
    (m) => m[1],
  );
  const playerIds = [
    ...String(res.body || "").matchAll(/\/player\/(\d+)</g),
  ].map((m) => m[1]);
  if (res.status !== 200 || teamIds.length === 0 || playerIds.length === 0) {
    throw new Error(
      `Couldn't read team and player ids from ${TARGET}/sitemap.xml (status ${res.status})`,
    );
  }
  return { teamIds, playerIds };
}

export default function (data) {
  pickAction().run(data);
  sleep(
    THINK_TIME_MIN_SECONDS +
      Math.random() * (THINK_TIME_MAX_SECONDS - THINK_TIME_MIN_SECONDS),
  );
}

// The results for one submetric filter ("kind:page" for all pages, "name:/teams" for one)
function stats(metrics, filter, durationSeconds) {
  const values = (name) => metrics[`${name}{${filter}}`]?.values ?? {};
  const count = (name) => Math.round(values(name).count ?? 0);
  const duration = (stat) =>
    (values("http_req_duration")[stat] ?? 0).toFixed(1);
  const requests = count("http_reqs");
  return {
    REQUESTS: requests,
    REQUESTS_PER_SECOND: (requests / durationSeconds).toFixed(2),
    P50_MS: duration("med"),
    P95_MS: duration("p(95)"),
    P99_MS: duration("p(99)"),
    MAX_MS: duration("max"),
    HTTP_5XX: count("http_5xx"),
    HTTP_429: count("http_429"),
    OTHER_ERRORS: count("other_errors"),
  };
}

// Writes the inputs and all-pages results as shell variables, and the per-page results as SQL
// (run inside save-load-test.sh's transaction, where :run_id is the new run's id)
export function handleSummary(data) {
  const m = data.metrics;
  const finished = new Date();
  const started = new Date(finished.getTime() - data.state.testRunDurationMs);
  const durationSeconds = Math.max(data.state.testRunDurationMs / 1000, 1);
  const pageMix = ACTIONS.map(
    (a) => `${a.label} ${Math.round((a.weight / TOTAL_WEIGHT) * 100)}%`,
  ).join(", ");

  const result = {
    PRESET: PRESET_NAME,
    TARGET,
    MAX_VUS: PRESET.maxVus,
    RAMP_UP_SECONDS: PRESET.rampUpSeconds,
    HOLD_SECONDS: PRESET.holdSeconds,
    RAMP_DOWN_SECONDS: PRESET.rampDownSeconds,
    THINK_TIME_MIN_SECONDS,
    THINK_TIME_MAX_SECONDS,
    PAGE_MIX: pageMix,
    STARTED_UTC: started.toISOString(),
    FINISHED_UTC: finished.toISOString(),
    ...stats(m, "kind:page", durationSeconds),
  };
  const env = Object.entries(result)
    .map(([key, value]) => `${key}='${String(value).replace(/'/g, "")}'`)
    .join("\n");

  // Names with no requests (say, a new name in a short run) get no row
  const pages = REQUEST_NAMES.map((name) => ({
    PAGE: name,
    ...stats(m, `name:${name}`, durationSeconds),
  })).filter((p) => p.REQUESTS > 0);
  // Page names are the fixed ones above, so they're safe to put in a SQL string
  const pagesSql = pages
    .map(
      (p) =>
        `INSERT INTO "LoadTestRunPage" ("LoadTestRunId", "Page", "Requests", "RequestsPerSecond", ` +
        `"P50Ms", "P95Ms", "P99Ms", "MaxMs", "Http5xx", "Http429", "OtherErrors") VALUES (:run_id, ` +
        `'${p.PAGE}', ${p.REQUESTS}, ${p.REQUESTS_PER_SECOND}, ${p.P50_MS}, ${p.P95_MS}, ` +
        `${p.P99_MS}, ${p.MAX_MS}, ${p.HTTP_5XX}, ${p.HTTP_429}, ${p.OTHER_ERRORS});`,
    )
    .join("\n");

  return {
    stdout: JSON.stringify({ ...result, pages }, null, 2) + "\n",
    "/out/result.env": env + "\n",
    "/out/pages.sql": pagesSql + "\n",
  };
}
