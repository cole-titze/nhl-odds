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
// 4. Games feed focus order: Tab out of the filter bar must land on the day
//    the page opened on, not on an off-screen card from days earlier
//
// Set CHROMIUM_PATH to use an existing Chromium instead of Playwright's.
import { spawn } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { chromium } from 'playwright';
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

const LOGO_SVG =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><circle cx="5" cy="5" r="5"/></svg>';

async function startPreview() {
  const server = spawn('npx', ['vite', 'preview', '--port', String(PORT), '--strictPort'], {
    stdio: 'ignore',
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
  throw new Error('vite preview did not start; did you run `npm run build`?');
}

async function newContext(browser, theme, viewport) {
  // Reduced motion zeroes out the site's color transitions (see index.css), so
  // axe never samples a color halfway through one
  const ctx = await browser.newContext({ viewport, reducedMotion: 'reduce' });
  await ctx.addInitScript((t) => {
    localStorage.setItem('theme', t);
    localStorage.removeItem('themeState');
  }, theme);
  await ctx.route('**/api/**', (route) => route.fulfill({ json: mockApi(route.request().url()) }));
  await ctx.route('https://assets.nhle.com/**', (route) =>
    route.fulfill({ contentType: 'image/svg+xml', body: LOGO_SVG }),
  );
  await ctx.route('https://fonts.*/**', (route) => route.abort());
  return ctx;
}

async function load(page, path) {
  await page.goto(BASE + path, { waitUntil: 'networkidle' });
  await page.waitForTimeout(500);
}

async function runAxe(page) {
  await page.addScriptTag({ content: axeSource });
  return page.evaluate(async (tags) => {
    const result = await window.axe.run(document, { runOnly: tags });
    return result.violations.flatMap((v) =>
      v.nodes.map(
        (n) => `${v.id} (${v.impact}) ${v.helpUrl}\n      ${n.target.join(' ')}\n      ${n.failureSummary}`,
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

async function tabThrough(page) {
  const problems = [];
  for (let i = 0; i < MAX_TABS; i++) {
    await page.keyboard.press('Tab');
    const problem = await page.evaluate(checkFocusVisible);
    if (problem && !problems.includes(problem)) problems.push(problem);
  }
  return problems;
}

function report(label, problems) {
  console.log(`${problems.length === 0 ? 'PASS' : 'FAIL'}  ${label}`);
  for (const p of problems) console.log(`    - ${p}`);
  return problems.length;
}

const server = await startPreview();
const browser = await chromium.launch(
  process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH } : {},
);
let total = 0;

try {
  // 1. axe
  for (const [viewportName, viewport] of Object.entries(VIEWPORTS)) {
    for (const theme of THEMES) {
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
      if (viewportName === 'phone') {
        await load(page, '/about');
        await page.locator('header button[aria-controls="mobile-menu"]').click();
        total += report(`axe [${viewportName} ${theme}] mobile menu open`, await runAxe(page));
      }
      await ctx.close();
    }
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

  // 4. Games feed focus order
  for (const [viewportName, viewport] of Object.entries(VIEWPORTS)) {
    const ctx = await newContext(browser, 'light', viewport);
    const page = await ctx.newPage();
    await load(page, '/');
    // Focus the filter bar's last control, as if the user had tabbed to it
    await page.evaluate(() => {
      const tabbable = [
        ...document.querySelectorAll('#games-filter-bar :is(a[href], button, input, select)'),
      ];
      tabbable[tabbable.length - 1].focus();
    });
    const before = await page.evaluate(() => window.scrollY);
    await page.keyboard.press('Tab');
    const result = await page.evaluate(
      ([anchor, scrollBefore]) => {
        const section = document.activeElement?.closest('section');
        if (section?.id === `date-${anchor}`) return null;
        return `Tab from the filter bar focused ${section ? section.id : document.activeElement?.tagName} (expected date-${anchor}); scroll ${Math.round(scrollBefore)} -> ${Math.round(window.scrollY)}`;
      },
      [ANCHOR_DATE, before],
    );
    total += report(`feed focus order [${viewportName}] /`, result ? [result] : []);
    await ctx.close();
  }
} finally {
  await browser.close();
  server.kill();
}

console.log(`\n${total} accessibility problem(s)`);
process.exit(total === 0 ? 0 : 1);
