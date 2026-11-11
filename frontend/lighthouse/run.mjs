// Lighthouse check: scores one page of each major type (the sitemap's pages
// plus a Game page) for SEO, accessibility, best practices and performance.
//
//   npm run build && npm run lighthouse
//     Serves the production build with the a11y scan's mocked API (a small
//     HTTP server, so pages render on the server the way crawlers get them)
//     and fails if any page's SEO score is under the threshold below. The
//     other categories only warn: axe already gates accessibility, and
//     performance on shared CI runners varies too much to gate on.
//
//   LIGHTHOUSE_URL=https://nhl.half-built.com npm run lighthouse
//     Scores the live site instead (the weekly report). Team and game pages
//     come from its sitemap and games API. Never fails on scores, only reports.
//
// Each page is scored LIGHTHOUSE_RUNS times (default 3), and the median of each
// category counts. The scores go to the console and, in CI, to the job summary
// (GITHUB_STEP_SUMMARY); each page's HTML report goes to lighthouse/reports/.
// Lighthouse's default mobile emulation is used, since Google indexes the mobile
// page. Set CHROMIUM_PATH to use an existing Chromium instead of Playwright's.
import { spawn } from 'node:child_process';
import { appendFileSync, mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { createServer } from 'node:http';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import lighthouse from 'lighthouse';
import { chromium } from 'playwright';
import { mockApi, pagesFor } from '../a11y/fixtures.mjs';

const LIVE_URL = process.env.LIGHTHOUSE_URL?.replace(/\/$/, '') || undefined;
const RUNS = Number(process.env.LIGHTHOUSE_RUNS) || 3;
const PORT = 4180;
const API_PORT = 4181;
const DEBUG_PORT = 9223;
const REPORT_DIR = join(dirname(fileURLToPath(import.meta.url)), 'reports');

// Minimum median score per category. `error` fails the run (local build only;
// the live report never fails), `warn` only flags it.
const THRESHOLDS = {
  seo: { min: 0.95, level: 'error' },
  accessibility: { min: 0.9, level: 'warn' },
  'best-practices': { min: 0.9, level: 'warn' },
  performance: { min: 0.8, level: 'warn' },
};
const CATEGORIES = Object.keys(THRESHOLDS);

// /admin and /search are noindex on purpose, so their SEO scores mean nothing
const NOINDEX = ['/admin', '/search'];
const localPages = () => pagesFor().filter((p) => !NOINDEX.includes(p.split('?')[0]));

// A rollout or a Cloudflare hiccup can answer one request with an error page,
// so each live lookup is retried a few times and fails naming the status
async function fetchLive(url) {
  let last;
  for (let attempt = 1; attempt <= 4; attempt++) {
    try {
      const res = await fetch(url);
      if (res.ok) return res;
      last = `HTTP ${res.status}`;
    } catch (e) {
      last = e.message;
    }
    if (attempt < 4) await new Promise((r) => setTimeout(r, 15_000));
  }
  throw new Error(`${url} failed 4 times (last: ${last})`);
}

async function livePages(base) {
  const sitemap = await (await fetchLive(`${base}/sitemap.xml`)).text();
  const paths = [...sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)].map((m) => new URL(m[1]).pathname);
  const team = paths.find((p) => p.startsWith('/team/'));
  if (!team) throw new Error(`no /team/ page in ${base}/sitemap.xml`);
  // The newest played game of the last two weeks
  const day = (offset) => new Date(Date.now() + offset * 864e5).toISOString().slice(0, 10);
  const params = new URLSearchParams({ startDate: day(-14), endDate: day(0) });
  const games = await (
    await fetchLive(`${base}/api/GameOdds/GetGameOddsInDateRange?${params}`)
  ).json();
  const game = games.filter((g) => g.hasBeenPlayed).at(-1) ?? games.at(-1);
  if (!game) throw new Error('no games in the last two weeks to pick a Game page from');
  const player = paths.find((p) => p.startsWith('/player/'));
  if (!player) throw new Error(`no /player/ page in ${base}/sitemap.xml`);
  // Search is noindex, so only its performance and accessibility scores mean anything
  return [
    '/',
    '/teams',
    team,
    `/game/${game.id}`,
    player,
    '/strategies',
    '/track-record',
    '/disagreements',
    '/search?q=McDavid',
    '/about',
  ];
}

async function waitFor(url, what) {
  for (let i = 0; i < 60; i++) {
    try {
      if ((await fetch(url)).ok) return;
    } catch {
      /* not up yet */
    }
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error(`${what} did not start`);
}

// The production server, with its API (server-side loaders and the /api proxy)
// pointed at the mocked one
async function startLocal() {
  const inUse = await fetch(`http://localhost:${PORT}`).then(
    () => true,
    () => false,
  );
  if (inUse) throw new Error(`port ${PORT} is already in use`);
  const api = createServer((req, res) => {
    res.setHeader('Content-Type', 'application/json');
    res.end(JSON.stringify(mockApi(`http://localhost${req.url}`)));
  }).listen(API_PORT);
  const server = spawn(process.execPath, ['server.mjs'], {
    stdio: ['ignore', 'ignore', 'inherit'],
    env: { ...process.env, PORT: String(PORT), API_URL: `http://127.0.0.1:${API_PORT}` },
  });
  try {
    await waitFor(`http://localhost:${PORT}/healthz`, 'server.mjs (did you run `npm run build`?)');
  } catch (e) {
    server.kill();
    api.close();
    throw e;
  }
  return () => {
    server.kill();
    api.close();
  };
}

const median = (xs) => {
  const s = [...xs].sort((a, b) => a - b);
  return s[Math.floor(s.length / 2)];
};

// Scores one page RUNS times, each in a fresh browser. Returns the median of each
// category, the report of the run closest to the median performance, and the
// SEO audits that didn't pass in that run.
async function score(url) {
  const runs = [];
  for (let i = 0; i < RUNS; i++) {
    const browser = await chromium.launch({
      executablePath: process.env.CHROMIUM_PATH || undefined,
      args: [`--remote-debugging-port=${DEBUG_PORT}`, '--no-sandbox'],
    });
    try {
      const result = await lighthouse(
        url,
        { port: DEBUG_PORT, output: 'html', logLevel: 'error', onlyCategories: CATEGORIES },
        undefined,
      );
      if (result?.lhr.runtimeError) throw new Error(result.lhr.runtimeError.message);
      runs.push(result);
    } finally {
      await browser.close();
    }
  }
  const scores = Object.fromEntries(
    CATEGORIES.map((c) => [c, median(runs.map((r) => r.lhr.categories[c].score ?? 0))]),
  );
  const pick =
    runs.find((r) => r.lhr.categories.performance.score === scores.performance) ?? runs[0];
  const seoIssues = pick.lhr.categories.seo.auditRefs
    .map((ref) => pick.lhr.audits[ref.id])
    .filter((a) => a.scoreDisplayMode === 'binary' && a.score !== 1)
    .map((a) => a.title);
  return { scores, report: pick.report, seoIssues };
}

const pct = (s) => Math.round(s * 100);

async function main() {
  const base = LIVE_URL ?? `http://localhost:${PORT}`;
  const stop = LIVE_URL ? () => {} : await startLocal();
  const results = [];
  try {
    const pages = LIVE_URL ? await livePages(LIVE_URL) : localPages();
    rmSync(REPORT_DIR, { recursive: true, force: true });
    mkdirSync(REPORT_DIR, { recursive: true });
    for (const path of pages) {
      process.stdout.write(`${path} ... `);
      const r = await score(base + path);
      const file = `${path === '/' ? 'games' : path.slice(1).replaceAll('/', '-')}.html`;
      writeFileSync(join(REPORT_DIR, file), r.report);
      results.push({ path, ...r });
      console.log(CATEGORIES.map((c) => `${c} ${pct(r.scores[c])}`).join(', '));
    }
  } finally {
    stop();
  }

  const failures = [];
  const warnings = [];
  for (const { path, scores, seoIssues } of results) {
    for (const [c, { min, level }] of Object.entries(THRESHOLDS)) {
      if (scores[c] >= min) continue;
      let msg = `${path}: ${c} ${pct(scores[c])} < ${pct(min)}`;
      if (c === 'seo' && seoIssues.length) msg += ` (${seoIssues.join('; ')})`;
      (level === 'error' && !LIVE_URL ? failures : warnings).push(msg);
    }
  }

  const summary = [
    `## Lighthouse${LIVE_URL ? ` (${LIVE_URL})` : ' (production build, mocked API)'}`,
    '',
    `Median of ${RUNS} runs, mobile. Minimums: ${CATEGORIES.map((c) => `${c} ${pct(THRESHOLDS[c].min)}`).join(', ')}.`,
    '',
    `| Page | ${CATEGORIES.join(' | ')} |`,
    `|---|${CATEGORIES.map(() => '---:').join('|')}|`,
    ...results.map(
      ({ path, scores }) =>
        `| \`${path}\` | ${CATEGORIES.map((c) => `${scores[c] < THRESHOLDS[c].min ? '⚠️ ' : ''}${pct(scores[c])}`).join(' | ')} |`,
    ),
    '',
    ...failures.map((f) => `- ❌ ${f}`),
    ...warnings.map((w) => `- ⚠️ ${w}`),
    '',
  ].join('\n');
  if (process.env.GITHUB_STEP_SUMMARY) appendFileSync(process.env.GITHUB_STEP_SUMMARY, summary);

  for (const w of warnings) console.log(`warning: ${w}`);
  for (const f of failures) console.error(`FAIL: ${f}`);
  console.log(`Reports: ${REPORT_DIR}`);
  if (failures.length) process.exit(1);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
