// End-to-end smoke test: every page of the production frontend against the real Web API and
// a Postgres database holding seed.sql. Started by run.sh, which sets E2E_BASE.
//
// The a11y scan covers the same pages with hand-written API mocks; this catches what those
// can't: the API's real JSON drifting from what the pages read, server-side loaders failing
// against the real API, and SQL the API can't run. Per page it checks the status code, that
// the server-rendered HTML already holds the page's data, the same text once hydrated, and
// that the browser saw no errors (console, uncaught, hydration, failed API calls).
import { chromium } from 'playwright';

const BASE = process.env.E2E_BASE || 'http://127.0.0.1:4299';

// Same rule as src/utils/season.ts and seed.sql
const now = new Date();
const SEASON =
  now >= new Date(Date.UTC(now.getUTCFullYear(), 8, 20))
    ? now.getUTCFullYear()
    : now.getUTCFullYear() - 1;
const gameId = (n) => SEASON * 1_000_000 + 20_000 + n;

// seed.sql: games 1-3 played, 4 is today's game day, 5 the day after
const PAGES = [
  { path: '/', title: 'Games', html: ['Oilers', 'Maple Leafs'], text: ['Oilers'] },
  {
    path: '/teams',
    title: 'Teams',
    html: ['Flames'],
    text: ['Maple Leafs', 'Flames', 'Oilers', 'Bruins'],
  },
  {
    path: '/team/10',
    title: 'Toronto Maple Leafs',
    html: ['Toronto Maple Leafs'],
    text: ['Toronto Maple Leafs', '1-0-0', 'Bruins', 'Oilers'],
  },
  {
    path: `/game/${gameId(1)}`,
    title: 'Boston Bruins at Toronto Maple Leafs',
    html: ['Boston Bruins at Toronto Maple Leafs', 'DraftKings'],
    text: ['Final', 'DraftKings', 'Kalshi', 'Correct'],
  },
  {
    path: `/game/${gameId(5)}`,
    title: 'Calgary Flames at Toronto Maple Leafs',
    html: ['Calgary Flames at Toronto Maple Leafs'],
    text: ['VS', 'DraftKings'],
  },
  {
    path: '/game/1',
    status: 404,
    title: 'Game not found',
    text: ['Game not found.'],
    allowApi: [404],
  },
  { path: '/strategies', title: 'Strategies', text: ['Strategies'] },
  // Server-rendered from the season's games: the seed's played games fill the record
  {
    path: '/disagreements',
    title: 'Model vs Market',
    html: ['Model vs Market', 'biggest disagreements'],
    text: ['Model vs Market', 'biggest disagreements', 'Every played game, by gap size'],
  },
  // The seed's three played games were all predicted the day before, so both views show them
  {
    path: '/track-record',
    title: 'Track Record',
    text: ['Track Record', 'Calibration', 'Against each market', 'DraftKings'],
  },
  {
    path: '/track-record?live=1',
    title: 'Track Record',
    text: ['Calibration', 'Saved before puck drop'],
  },
  // A past season with no games says so for that season, not "yet this season"
  {
    path: '/track-record?season=2015',
    title: 'Track Record',
    text: ['No games in 2015-16 had a prediction.'],
  },
  { path: '/about', title: 'About', text: ['About NHL Odds'] },
  // Without the admin key its endpoints answer 403, which the page shows as a prompt
  { path: '/admin', title: 'Admin', allowApi: [401, 403], allowAlert: true },
];

const failures = [];
const fail = (where, message) => failures.push(`${where}: ${message}`);

async function checkResource(path, check) {
  const res = await fetch(BASE + path);
  const body = await res.text();
  const problem = check(res, body);
  if (problem) fail(path, problem);
  else console.log(`PASS  ${path}`);
}

async function checkPage(browser, page) {
  const where = page.path;
  const before = failures.length;

  // 1. The server's own render: status and data, before any JavaScript runs
  const res = await fetch(BASE + page.path);
  const html = await res.text();
  if (res.status !== (page.status ?? 200))
    fail(where, `status ${res.status}, expected ${page.status ?? 200}`);
  for (const t of page.html ?? []) {
    if (!html.includes(t)) fail(where, `server-rendered HTML is missing "${t}"`);
  }

  // 2. In the browser, hydrated, with every error recorded
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const tab = await context.newPage();
  const errors = [];
  tab.on('console', (m) => m.type() === 'error' && errors.push(`console: ${m.text()}`));
  tab.on('pageerror', (e) => errors.push(`uncaught: ${e.message}`));
  tab.on('response', (r) => {
    const url = new URL(r.url());
    if (
      url.origin === new URL(BASE).origin &&
      url.pathname.startsWith('/api/') &&
      r.status() >= 400
    ) {
      if (!(page.allowApi ?? []).includes(r.status()))
        errors.push(`API ${r.status()} ${url.pathname}${url.search}`);
    }
  });
  tab.on('requestfailed', (r) => {
    // Navigating away cancels in-flight requests; those aren't failures
    if (r.failure()?.errorText !== 'net::ERR_ABORTED')
      errors.push(`request failed: ${r.url()} ${r.failure()?.errorText}`);
  });

  await tab.goto(BASE + page.path, { waitUntil: 'networkidle' });
  const title = await tab.title();
  if (!title.startsWith(page.title))
    fail(where, `title "${title}", expected it to start with "${page.title}"`);
  // innerText applies CSS text-transform (FINAL), so compare without case
  const body = (await tab.locator('body').innerText()).toLowerCase();
  for (const t of page.text ?? []) {
    if (!body.includes(t.toLowerCase())) fail(where, `page doesn't show "${t}"`);
  }
  if (!page.allowAlert && (await tab.locator('[role=alert]').count()) > 0) {
    // Expected for the unknown game; anything else is a load failure
    const alert = await tab.locator('[role=alert]').first().innerText();
    if (!(page.text ?? []).some((t) => alert.includes(t)))
      fail(where, `shows an error: "${alert}"`);
  }
  for (const e of errors.filter((e) => !(page.status === 404 && /status of 404/.test(e))))
    fail(where, e);
  await context.close();

  if (failures.length === before) console.log(`PASS  ${where}`);
}

const browser = await chromium.launch(
  process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH } : {},
);
try {
  for (const page of PAGES) await checkPage(browser, page);

  await checkResource('/sitemap.xml', (res, body) =>
    res.status !== 200
      ? `status ${res.status}`
      : body.includes('/team/10')
        ? null
        : 'no /team/10 entry',
  );
  // The API through the frontend's /api proxy, the way the browser reaches it
  await checkResource(`/api/GameOdds/GetGame?gameId=${gameId(1)}`, (res, body) => {
    if (res.status !== 200) return `status ${res.status}`;
    const game = JSON.parse(body);
    return game.homeTeam?.teamName === 'Maple Leafs' && game.bookmakerOdds?.length === 2
      ? null
      : `unexpected game JSON: ${body.slice(0, 200)}`;
  });
} finally {
  await browser.close();
}

if (failures.length) {
  console.error(`\n${failures.length} problem(s):`);
  for (const f of failures) console.error(`FAIL  ${f}`);
  process.exit(1);
}
console.log('\nSmoke test passed');
