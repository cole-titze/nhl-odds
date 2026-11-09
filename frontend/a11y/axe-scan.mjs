// Accessibility scan: serves the production build with a mocked API and checks
// every page. Exits non-zero if anything is flagged.
//
//   npm run build && npm run a11y
//
// 1. axe-core WCAG 2.2 A/AA rules (plus best practices) on every page, in
//    every theme, at desktop and phone widths (with the mobile menu open once)
// 2. Reflow (1.4.10): at 320px wide nothing may overflow the viewport, except
//    inside its own scroll container (wide data tables are allowed to scroll)
// 3. Focus not obscured (2.4.11): tab through each page at desktop and phone
//    widths; every focused element must be visible, not hidden behind the
//    sticky navbar, filter bar or other fixed elements
// 4. Games feed focus order: Tab out of the filter bar must land on the first
//    game fully on screen, without scrolling (the feed starts days before
//    the date the page opens on, so the next card in DOM order is off screen)
// 5. Tabbing between the Games filter bar's controls must not scroll the feed
// 6. Navbar search suggestions: typing opens the list, arrow keys move
//    through it (the box keeps focus), Escape closes it and Enter on a
//    suggestion opens it, a 503 shows "search isn't available" and an empty
//    answer a "no matches" line above the "see all results" row; axe also
//    scans the open list and the messages (desktop)
// 7. Loading states, with the search API slowed down: the search page shows
//    result placeholders for a first search and a progress bar over old
//    results (still visible with reduced motion), the navbar box a spinner,
//    each announced once ("Searching…"); axe scans each. With the API at full
//    speed, nothing loading-shaped appears on the search page (indicators wait
//    300 ms); the navbar box's spinner shows from the first keystroke anyway
//
// axe runs in Chromium only (its rules don't depend on the engine); checks
// 2-7 run in every browser in A11Y_BROWSERS (comma-separated, default
// chromium,firefox,webkit), since scrolling and focus behave differently in
// each. WebKit is driven with Option+Tab, as Safari's plain Tab skips links
// and buttons. Set CHROMIUM_PATH to use an existing Chromium instead of
// Playwright's.
// A11Y_PARTS (axe,checks) and A11Y_AXE_SHARD (e.g. 2/4) split the scan across
// CI jobs; by default one run does everything.
import { spawn } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { chromium, firefox, webkit } from 'playwright';
import { ANCHOR_DATE, THEMES, pagesFor, mockApi } from './fixtures.mjs';

const require = createRequire(import.meta.url);
const axeSource = readFileSync(require.resolve('axe-core/axe.min.js'), 'utf8');
const PORT = 4179;
const BASE = `http://localhost:${PORT}`;
const AXE_TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'];
const VIEWPORTS = {
  desktop: { width: 1280, height: 900 },
  phone: { width: 375, height: 812 },
};
const MAX_TABS = 80;
// How long check 7 holds each search request, so the loading state can be inspected
const SLOW_API_MS = 2500;
const ENGINES = { chromium, firefox, webkit };
const BROWSERS = (process.env.A11Y_BROWSERS || Object.keys(ENGINES).join(','))
  .split(',')
  .map((b) => b.trim());
for (const b of BROWSERS) {
  if (!ENGINES[b]) throw new Error(`unknown browser "${b}" in A11Y_BROWSERS`);
}
// CI splits the scan across jobs: A11Y_PARTS picks the axe pass and/or the
// other checks; A11Y_AXE_SHARD ("2/4") runs axe for every 4th theme from the 2nd
const PARTS = (process.env.A11Y_PARTS || 'axe,checks').split(',').map((p) => p.trim());
const [shard, shards] = (process.env.A11Y_AXE_SHARD || '1/1').split('/').map(Number);
if (!(shard >= 1 && shard <= shards)) throw new Error('A11Y_AXE_SHARD must look like "2/4"');
const AXE_THEMES = THEMES.filter((_, i) => i % shards === shard - 1);
// Set per browser below
let engine = BROWSERS[0];
let TAB = 'Tab';

const LOGO_SVG =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><circle cx="5" cy="5" r="5"/></svg>';

async function startPreview() {
  // Anything already answering here (say a preview of another build left
  // running) would get scanned instead of this build
  const inUse = await fetch(BASE).then(
    () => true,
    () => false,
  );
  if (inUse) throw new Error(`port ${PORT} is already in use; stop whatever is serving ${BASE}`);
  // The production server, with an API address nothing answers on: the
  // loaders fail, so pages fetch in the browser, where the mocks answer
  const server = spawn(process.execPath, ['server.mjs'], {
    // stderr shows server-side render errors in the scan's output
    stdio: ['ignore', 'ignore', 'inherit'],
    env: { ...process.env, PORT: String(PORT), API_URL: 'http://127.0.0.1:9' },
  });
  for (let i = 0; i < 60; i++) {
    try {
      if ((await fetch(BASE)).ok) return server;
    } catch {
      /* not up yet */
    }
    await new Promise((r) => setTimeout(r, 500));
  }
  server.kill();
  throw new Error('server.mjs did not start; did you run `npm run build`?');
}

async function newContext(browser, theme, viewport) {
  // Reduced motion zeroes out the site's color transitions (see index.css), so
  // axe never samples a color halfway through one. The server's CSP would block
  // axe's injected script, so it's bypassed. Service workers are blocked: once the
  // site's worker (sw.ts) took control, WebKit sent some API requests past the
  // mock below to the real (absent) API, and the Games feed timed out waiting.
  const ctx = await browser.newContext({
    viewport,
    reducedMotion: 'reduce',
    bypassCSP: true,
    serviceWorkers: 'block',
  });
  await ctx.addInitScript((t) => {
    localStorage.setItem('theme', t);
    localStorage.removeItem('themeState');
    // Unlocks /admin in the production build so the full page (not the key form) gets scanned
    localStorage.setItem('adminKey', 'a11y');
  }, theme);
  await ctx.route('**/api/**', (route) => route.fulfill({ json: mockApi(route.request().url()) }));
  await ctx.route('https://assets.nhle.com/**', (route) =>
    route.fulfill({ contentType: 'image/svg+xml', body: LOGO_SVG }),
  );
  await ctx.route('https://fonts.*/**', (route) => route.abort());
  return ctx;
}

async function load(page, path) {
  const log = [];
  const onConsole = (m) => log.push(`${m.type()}: ${m.text().slice(0, 300)}`);
  const onError = (e) => log.push(`pageerror: ${e.message.slice(0, 300)}`);
  const onRequest = (r) => r.url().includes('/api/') && log.push(`request ${r.url()}`);
  page.on('console', onConsole);
  page.on('pageerror', onError);
  page.on('request', onRequest);
  await page.goto(BASE + path, { waitUntil: 'networkidle' });
  // The server can't reach the mocked API, so the games feed loads in the
  // browser after hydration; slow runners can still be on the skeleton here
  if (path === '/') {
    await page.waitForSelector('#games-feed a[href]').catch(async (err) => {
      // Rare in CI WebKit only: say what the page was doing
      const main = await page.evaluate(() => ({
        hydrated: !!window.__reactRouterRouteModules,
        text: document.querySelector('main')?.innerText.slice(0, 300),
      }));
      console.log(`games feed never showed: ${JSON.stringify(main)}`);
      log.forEach((l) => console.log(`    ${l}`));
      throw err;
    });
  }
  page.off('console', onConsole);
  page.off('pageerror', onError);
  page.off('request', onRequest);
  await page.waitForTimeout(500);
}

async function runAxe(page) {
  await page.addScriptTag({ content: axeSource });
  return page.evaluate(async (tags) => {
    const result = await window.axe.run(document, { runOnly: tags });
    return result.violations.flatMap((v) =>
      v.nodes.map(
        (n) =>
          `${v.id} (${v.impact}) ${v.helpUrl}\n      ${n.target.join(' ')}\n      ${n.failureSummary}`,
      ),
    );
  }, AXE_TAGS);
}

// Elements sticking out past the viewport edge that aren't inside their own
// scroll/clip container (the page shell's overflow-x-clip doesn't count: it
// would cut the content off rather than let people scroll to it).
function findReflowProblems() {
  const width = window.innerWidth;
  const contained = (el) => {
    for (let a = el.parentElement; a && a !== document.body; a = a.parentElement) {
      if (a.classList.contains('app-shell')) return false;
      if (getComputedStyle(a).overflowX !== 'visible') return true;
    }
    return false;
  };
  const problems = [];
  for (const el of document.body.querySelectorAll('*')) {
    const rect = el.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) continue;
    if (el.closest('[aria-hidden="true"], .sr-only, .scene')) continue;
    if (rect.right <= width + 1 && rect.left >= -1) continue;
    if (contained(el)) continue;
    const name = `${el.tagName.toLowerCase()}${el.id ? '#' + el.id : ''}.${[...el.classList].slice(0, 3).join('.')}`;
    problems.push(`${name} spans ${Math.round(rect.left)}..${Math.round(rect.right)}px`);
  }
  return problems.slice(0, 10);
}

// Is the focused element at least partly visible? Samples its center and
// corners; any one landing on the element itself (not a sticky bar or other
// overlay) counts as visible.
function checkFocusVisible() {
  const el = document.activeElement;
  if (!el || el === document.body || el.id === 'main') return null;
  const r = el.getBoundingClientRect();
  if (r.width === 0 || r.height === 0) return null;
  const inset = 2;
  const points = [
    [r.left + r.width / 2, r.top + r.height / 2],
    [r.left + inset, r.top + inset],
    [r.right - inset, r.top + inset],
    [r.left + inset, r.bottom - inset],
    [r.right - inset, r.bottom - inset],
  ];
  const visible = points.some(([x, y]) => {
    if (x < 0 || y < 0 || x >= window.innerWidth || y >= window.innerHeight) return false;
    const hit = document.elementFromPoint(x, y);
    return hit && (hit === el || el.contains(hit) || hit.contains(el));
  });
  if (visible) return null;
  const label = (el.getAttribute('aria-label') || el.textContent || '').trim().slice(0, 40);
  const hit = document.elementFromPoint(
    Math.min(Math.max(r.left + r.width / 2, 0), window.innerWidth - 1),
    Math.min(Math.max(r.top + r.height / 2, 0), window.innerHeight - 1),
  );
  const by = hit?.closest('header, [id], button') ?? hit;
  return `<${el.tagName.toLowerCase()}> "${label}" hidden (top ${Math.round(r.top)}px) under ${
    by ? by.tagName.toLowerCase() + (by.id ? '#' + by.id : '') : 'nothing (off screen)'
  }`;
}

// WebKit can scroll a newly focused element into view a little after the key press (seen on CI's
// Linux WebKit, Playwright 1.64), so give it up to 500 ms to come into view
async function focusProblem(page) {
  let problem = await page.evaluate(checkFocusVisible);
  for (let i = 0; problem && i < 20; i++) {
    await page.waitForTimeout(25);
    problem = await page.evaluate(checkFocusVisible);
  }
  return problem;
}

async function tabThrough(page) {
  const problems = [];
  for (let i = 0; i < MAX_TABS; i++) {
    await page.keyboard.press(TAB);
    const problem = await focusProblem(page);
    if (problem && !problems.includes(problem)) problems.push(problem);
  }
  return problems;
}

/** Types into the navbar search box and waits for its suggestion list. */
async function openSuggestions(page) {
  await page.locator('header [role="combobox"]').fill('leafs');
  await page.locator('header [role="listbox"]:not([hidden])').waitFor({ timeout: 5000 });
}

/** Problems with the navbar suggestion list's keyboard behavior (check 6). */
async function suggestionProblems(page) {
  const problems = [];
  const box = page.locator('header [role="combobox"]');
  await openSuggestions(page);
  if ((await box.getAttribute('aria-expanded')) !== 'true')
    problems.push('aria-expanded is not true with the list open');
  await page.keyboard.press('ArrowDown');
  const activeId = await box.getAttribute('aria-activedescendant');
  const selected =
    activeId && (await page.locator(`[id="${activeId}"]`).getAttribute('aria-selected'));
  if (selected !== 'true') problems.push('ArrowDown did not select the first suggestion');
  if (!(await box.evaluate((el) => el === document.activeElement)))
    problems.push('ArrowDown moved focus out of the box');
  await page.keyboard.press('Escape');
  if ((await box.getAttribute('aria-expanded')) !== 'false')
    problems.push('Escape did not close the list');
  // ArrowDown reopens it on the first suggestion
  await page.keyboard.press('ArrowDown');
  if ((await box.getAttribute('aria-expanded')) !== 'true')
    problems.push('ArrowDown did not reopen the list');
  await page.keyboard.press('Enter');
  await page
    .waitForURL('**/team/10', { timeout: 5000 })
    .catch(() => problems.push(`Enter went to ${page.url()}, not /team/10`));
  // The box empties in the render that follows the navigation, which can land a little after the URL
  // changes (the route's code loads first on a slow runner), so wait for it rather than read it once
  await box
    .evaluate(
      (el) =>
        new Promise((resolve, reject) => {
          const end = Date.now() + 5000;
          (function poll() {
            if (el.value === '') resolve();
            else if (Date.now() > end) reject(new Error('kept'));
            else setTimeout(poll, 25);
          })();
        }),
    )
    .catch(() => problems.push('the box kept its text after opening a suggestion'));
  return problems;
}

function report(label, problems) {
  console.log(`${problems.length === 0 ? 'PASS' : 'FAIL'}  ${engine}: ${label}`);
  for (const p of problems) console.log(`    - ${p}`);
  return problems.length;
}

const server = await startPreview();
// Stop the server however the scan ends, including a crash, so the next run
// doesn't find the port taken
process.on('exit', () => server.kill());
let browser;
let total = 0;

try {
  for (engine of BROWSERS) {
    TAB = engine === 'webkit' ? 'Alt+Tab' : 'Tab';
    browser = await ENGINES[engine].launch(
      engine === 'chromium' && process.env.CHROMIUM_PATH
        ? { executablePath: process.env.CHROMIUM_PATH }
        : {},
    );

    // 1. axe
    if (engine === 'chromium' && PARTS.includes('axe')) {
      for (const [viewportName, viewport] of Object.entries(VIEWPORTS)) {
        for (const theme of AXE_THEMES) {
          const ctx = await newContext(browser, theme, viewport);
          const page = await ctx.newPage();
          for (const path of pagesFor()) {
            await load(page, path);
            // Open collapsed sections so their contents get scanned too
            for (const button of await page.locator('main button[aria-expanded="false"]').all()) {
              await button.click({ timeout: 1000 }).catch(() => {});
            }
            await page.waitForTimeout(300);
            total += report(`axe [${viewportName} ${theme}] ${path}`, await runAxe(page));
          }
          if (viewportName === 'desktop') {
            await load(page, '/about');
            await openSuggestions(page);
            await page.keyboard.press('ArrowDown');
            total += report(
              `axe [${viewportName} ${theme}] search suggestions open`,
              await runAxe(page),
            );
          }
          if (viewportName === 'phone') {
            await load(page, '/about');
            await page.locator('header button[aria-controls="mobile-menu"]').click();
            total += report(`axe [${viewportName} ${theme}] mobile menu open`, await runAxe(page));
          }
          await ctx.close();
        }
      }
    }

    if (!PARTS.includes('checks')) {
      await browser.close();
      browser = undefined;
      continue;
    }

    // 2. Reflow at 320px
    {
      const ctx = await newContext(browser, 'light', { width: 320, height: 640 });
      const page = await ctx.newPage();
      for (const path of pagesFor()) {
        await load(page, path);
        total += report(`reflow [320px] ${path}`, await page.evaluate(findReflowProblems));
      }
      await ctx.close();
    }

    // 3. Focus not obscured
    for (const [viewportName, viewport] of Object.entries(VIEWPORTS)) {
      const ctx = await newContext(browser, 'light', viewport);
      const page = await ctx.newPage();
      for (const path of pagesFor()) {
        await load(page, path);
        total += report(`focus visible [${viewportName}] ${path}`, await tabThrough(page));
      }
      await ctx.close();
    }

    // 4. Games feed focus order, from where the page opens and from a scroll
    //    position that leaves a card peeking out from under the filter bar
    for (const [viewportName, viewport] of Object.entries(VIEWPORTS)) {
      for (const position of ['opened', 'card under bar']) {
        const ctx = await newContext(browser, 'light', viewport);
        const page = await ctx.newPage();
        await load(page, '/');
        if (position === 'card under bar') {
          await page.evaluate(() => {
            const bar = document.getElementById('games-filter-bar').getBoundingClientRect().bottom;
            const cards = [...document.querySelectorAll('#games-feed a[href]')];
            const next = cards.find((c) => c.getBoundingClientRect().top > bar + 50);
            const partly = cards[cards.indexOf(next) - 1];
            window.scrollBy(0, partly.getBoundingClientRect().bottom - (bar + 20));
          });
          await page.waitForTimeout(300);
        }
        // Focus the filter bar's last control, as if the user had tabbed to it
        await page.evaluate(() => {
          const tabbable = [
            ...document.querySelectorAll('#games-filter-bar :is(a[href], button, input, select)'),
          ];
          tabbable[tabbable.length - 1].focus();
        });
        const before = await page.evaluate(() => window.scrollY);
        await page.keyboard.press(TAB);
        const result = await page.evaluate(
          ([anchor, scrollBefore]) => {
            const el = document.activeElement;
            const feed = document.getElementById('games-feed');
            const barsBottom = document
              .getElementById('games-filter-bar')
              .getBoundingClientRect().bottom;
            const onScreen = [...feed.querySelectorAll('a[href], button:not([disabled])')].filter(
              (n) => {
                const r = n.getBoundingClientRect();
                return r.height > 0 && r.top >= barsBottom - 1 && r.top < window.innerHeight;
              },
            );
            const where = `${el?.closest('section')?.id ?? el?.tagName} (page opened on date-${anchor})`;
            const problems = [];
            if (Math.abs(window.scrollY - scrollBefore) > 1)
              problems.push(
                `page scrolled ${Math.round(scrollBefore)} -> ${Math.round(window.scrollY)}; focus went to ${where}`,
              );
            if (el !== onScreen[0])
              problems.push(`focus went to ${where}, not the first game fully on screen`);
            return problems;
          },
          [ANCHOR_DATE, before],
        );
        total += report(`feed focus order [${viewportName}, ${position}] /`, result);
        await ctx.close();
      }
    }

    // 5. Tabbing between the filter bar's controls must not scroll the feed
    //    (they're always on screen; a scroll-margin on them used to push the
    //    feed back a few days per Tab, and into loads of older games)
    for (const [viewportName, viewport] of Object.entries(VIEWPORTS)) {
      const ctx = await newContext(browser, 'light', viewport);
      const page = await ctx.newPage();
      await load(page, '/');
      const before = await page.evaluate(() => window.scrollY);
      await page.evaluate(() =>
        document.querySelector('#games-filter-bar :is(a[href], button, input, select)').focus(),
      );
      const problems = [];
      for (let i = 0; i < 12; i++) {
        const step = await page.evaluate((scrollBefore) => {
          const el = document.activeElement;
          if (!el?.closest('#games-filter-bar')) return { done: true };
          const label = (el.getAttribute('aria-label') || el.textContent || el.tagName).trim();
          return {
            moved:
              Math.abs(window.scrollY - scrollBefore) > 1
                ? `focusing "${label.slice(0, 30)}" scrolled ${Math.round(scrollBefore)} -> ${Math.round(window.scrollY)}`
                : null,
          };
        }, before);
        if (step.done) break;
        if (step.moved) {
          problems.push(step.moved);
          break;
        }
        await page.keyboard.press(TAB);
      }
      total += report(`filter bar tabbing keeps scroll [${viewportName}] /`, problems);
      await ctx.close();
    }

    // 6. Navbar search suggestions (the box is desktop-only; phones use /search)
    {
      const ctx = await newContext(browser, 'light', VIEWPORTS.desktop);
      const page = await ctx.newPage();
      await load(page, '/about');
      total += report(
        'search suggestions keyboard [desktop] /about',
        await suggestionProblems(page),
      );
      await ctx.close();
    }

    // 6b. Search down: the box says so instead of showing nothing
    {
      const ctx = await newContext(browser, 'light', VIEWPORTS.desktop);
      // Registered after newContext's mocks, so it wins for this URL
      await ctx.route('**/api/Search/Suggest**', (route) =>
        route.fulfill({ status: 503, json: {} }),
      );
      const page = await ctx.newPage();
      await load(page, '/about');
      const problems = [];
      await page.locator('header [role="combobox"]').fill('leafs');
      await page
        .locator('header')
        .getByText("Search isn't available right now")
        .first()
        .waitFor({ timeout: 5000 })
        .catch(() => problems.push('no "unavailable" message after a 503'));
      const status = await page.locator('header [role="status"]').textContent();
      if (!status?.includes("isn't available")) problems.push(`status line says "${status}"`);
      if (engine === 'chromium') problems.push(...(await runAxe(page)));
      total += report('search suggestions on a 503 [desktop] /about', problems);
      await ctx.close();
    }

    // 6c. Nothing matches: the box says so and still offers the full search
    {
      const ctx = await newContext(browser, 'light', VIEWPORTS.desktop);
      await ctx.route('**/api/Search/Suggest**', (route) =>
        route.fulfill({
          json: {
            query: new URL(route.request().url()).searchParams.get('q'),
            teams: [],
            players: [],
            games: [],
          },
        }),
      );
      const page = await ctx.newPage();
      await load(page, '/about');
      const problems = [];
      await page.locator('header [role="combobox"]').fill('zzqx');
      await page
        .locator('header')
        .getByText('No teams, players or games match')
        .first()
        .waitFor({ timeout: 5000 })
        .catch(() => problems.push('no "no matches" message for an empty answer'));
      if ((await page.locator('header [role="option"]').count()) !== 1)
        problems.push('the "see all results" row is missing from the zero state');
      const status = await page.locator('header [role="status"]').textContent();
      if (!status?.includes('No teams, players or games match'))
        problems.push(`status line says "${status}"`);
      if (engine === 'chromium') problems.push(...(await runAxe(page)));
      total += report('search suggestions with no matches [desktop] /about', problems);
      await ctx.close();
    }

    // 7. Loading states with a slow search API
    {
      const ctx = await newContext(browser, 'light', VIEWPORTS.desktop);
      // Registered after newContext's mocks, so it wins for these URLs
      await ctx.route('**/api/Search**', async (route) => {
        await new Promise((r) => setTimeout(r, SLOW_API_MS));
        // The page may have moved on (and closed the request) meanwhile
        await route.fulfill({ json: mockApi(route.request().url()) }).catch(() => {});
      });
      const page = await ctx.newPage();
      const status = page.locator('main [role="status"]');

      // First search from an empty page: placeholders, not a blank space
      await load(page, '/search');
      let problems = [];
      await page.locator('#search-page-input').fill('leafs');
      await page
        .locator('[data-loading="search"]')
        .waitFor({ timeout: SLOW_API_MS })
        .catch(() => problems.push('no result placeholders during a slow first search'));
      if ((await status.textContent()) !== 'Searching…')
        problems.push(`status line says "${await status.textContent()}", not "Searching…"`);
      if (engine === 'chromium') problems.push(...(await runAxe(page)));
      total += report('search page loading, first search [desktop] /search', problems);

      // A new search with results on screen: they stay, under a progress bar
      problems = [];
      await page.locator('#results-teams').waitFor({ timeout: SLOW_API_MS * 2 });
      await page.locator('#search-page-input').fill('bruins');
      const bar = page.locator('[data-loading="progress"]:not(.invisible)');
      await bar
        .waitFor({ timeout: SLOW_API_MS })
        .catch(() => problems.push('no progress bar over the old results'));
      const barWidth = await bar
        .locator('div')
        .evaluate((el) => el.getBoundingClientRect().width)
        .catch(() => 0);
      if (barWidth < 1) problems.push('the progress bar is empty with reduced motion');
      if ((await page.locator('#results-teams').count()) === 0)
        problems.push('the old results disappeared while searching');
      if ((await status.textContent()) !== 'Searching…')
        problems.push(`status line says "${await status.textContent()}", not "Searching…"`);
      if (engine === 'chromium') problems.push(...(await runAxe(page)));
      total += report('search page loading, with old results [desktop] /search', problems);

      // Navbar suggestions: a spinner where the search icon was
      problems = [];
      await load(page, '/about');
      await page.locator('header [role="combobox"]').fill('leafs');
      await page
        .locator('header [data-loading="suggestions"]')
        .waitFor({ timeout: SLOW_API_MS })
        .catch(() => problems.push('no spinner while suggestions load'));
      // The spinner shows at once; the status line waits LOADING_DELAY_MS so typing isn't read out per keystroke
      await page
        .locator('header [role="status"]', { hasText: /^Searching…$/ })
        .waitFor({ state: 'attached', timeout: SLOW_API_MS })
        .catch(async () =>
          problems.push(
            `status line says "${await page.locator('header [role="status"]').textContent()}", not "Searching…"`,
          ),
        );
      if (engine === 'chromium') problems.push(...(await runAxe(page)));
      total += report('search suggestions loading [desktop] /about', problems);
      await ctx.close();
    }

    // 7b. Fast answers show no loading indicator at all
    {
      const ctx = await newContext(browser, 'light', VIEWPORTS.desktop);
      const page = await ctx.newPage();
      await load(page, '/search');
      await page.evaluate(() => {
        window.__sawLoading = false;
        new MutationObserver(() => {
          if (document.querySelector('[data-loading]:not(.invisible)')) window.__sawLoading = true;
        }).observe(document.body, { subtree: true, childList: true, attributes: true });
      });
      await page.locator('#search-page-input').fill('leafs');
      await page.locator('#results-teams').waitFor({ timeout: 5000 });
      const problems = [];
      if (await page.evaluate(() => window.__sawLoading))
        problems.push('a loading indicator flashed on the search page');
      // The navbar box is the exception: its spinner shows from the first keystroke, even for fast answers,
      // so a slow answer never looks like no matches
      await load(page, '/about');
      await page.evaluate(() => {
        window.__sawLoading = false;
        new MutationObserver(() => {
          if (document.querySelector('header [data-loading="suggestions"]'))
            window.__sawLoading = true;
        }).observe(document.body, { subtree: true, childList: true, attributes: true });
      });
      await page.locator('header [role="combobox"]').fill('leafs');
      await page.locator('header [role="listbox"]:not([hidden])').waitFor({ timeout: 5000 });
      if (!(await page.evaluate(() => window.__sawLoading)))
        problems.push('no spinner in the navbar box while its suggestions loaded');
      total += report('loading indicators for fast answers [desktop] /search, /about', problems);
      await ctx.close();
    }

    await browser.close();
    browser = undefined;
  }
} finally {
  await browser?.close();
  server.kill();
}

console.log(`\n${total} accessibility problem(s)`);
process.exit(total === 0 ? 0 : 1);
