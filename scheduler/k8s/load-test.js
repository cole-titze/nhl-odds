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

// Share of page loads per page; the game and team pages pick a random game or team each time
const PAGE_MIX = [
  { name: "/", weight: 35, path: () => "/" },
  { name: "/game/:id", weight: 25, path: () => `/game/${randomGameId()}` },
  {
    name: "/team/:id",
    weight: 15,
    path: (teamIds) => `/team/${pick(teamIds)}`,
  },
  { name: "/teams", weight: 10, path: () => "/teams" },
  { name: "/strategies", weight: 10, path: () => "/strategies" },
  { name: "/about", weight: 5, path: () => "/about" },
];
const TOTAL_WEIGHT = PAGE_MIX.reduce((sum, p) => sum + p.weight, 0);

const http5xx = new Counter("http_5xx");
const http429 = new Counter("http_429");
const otherErrors = new Counter("other_errors");

// Stats are kept for all page loads (kind:page, which leaves out setup's sitemap request) and
// for each page; k6 only reports a tag's numbers when a threshold names it, so these always pass
const SUBMETRIC_FILTERS = [
  "kind:page",
  ...PAGE_MIX.map((p) => `name:${p.name}`),
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

function pickPage() {
  let roll = Math.random() * TOTAL_WEIGHT;
  for (const page of PAGE_MIX) {
    roll -= page.weight;
    if (roll < 0) return page;
  }
  return PAGE_MIX[0];
}

// The team ids come from the sitemap, which lists every team page
export function setup() {
  const res = http.get(`${TARGET}/sitemap.xml`, {
    responseType: "text",
    timeout: "30s",
  });
  const teamIds = [...String(res.body || "").matchAll(/\/team\/(\d+)</g)].map(
    (m) => m[1],
  );
  if (res.status !== 200 || teamIds.length === 0) {
    throw new Error(
      `Couldn't read team ids from ${TARGET}/sitemap.xml (status ${res.status})`,
    );
  }
  return { teamIds };
}

export default function (data) {
  const page = pickPage();
  const res = http.get(`${TARGET}${page.path(data.teamIds)}`, {
    // The web API rate-limits per CF-Connecting-IP, so each VU gets its own address
    // (198.18.0.0/15 is reserved for benchmarking) and is limited like one real visitor
    headers: {
      "CF-Connecting-IP": `198.18.${Math.floor(__VU / 256)}.${__VU % 256}`,
    },
    tags: { name: page.name, kind: "page" },
    timeout: "30s",
  });

  const tags = { name: page.name, kind: "page" };
  if (res.status >= 500) http5xx.add(1, tags);
  else if (res.status === 429) http429.add(1, tags);
  else if (res.status === 0 || res.status >= 400) otherErrors.add(1, tags);

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
  const pageMix = PAGE_MIX.map(
    (p) => `${p.name} ${Math.round((p.weight / TOTAL_WEIGHT) * 100)}%`,
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

  const pages = PAGE_MIX.map((p) => ({
    PAGE: p.name,
    ...stats(m, `name:${p.name}`, durationSeconds),
  }));
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
