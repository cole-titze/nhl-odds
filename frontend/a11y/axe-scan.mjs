// Accessibility scan: serves the production build with a mocked API, loads
// every page in every theme, and runs axe-core's WCAG 2.2 A/AA rules (plus
// best practices). Exits non-zero if anything is flagged.
//
//   npm run build && npm run a11y
//
// Set CHROMIUM_PATH to use an existing Chromium instead of Playwright's.
import { spawn } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { chromium } from 'playwright';
import { THEMES, pagesFor, mockApi } from './fixtures.mjs';

const require = createRequire(import.meta.url);
const axeSource = readFileSync(require.resolve('axe-core/axe.min.js'), 'utf8');
const PORT = 4179;
const BASE = `http://localhost:${PORT}`;
const AXE_TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'];

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

const server = await startPreview();
const browser = await chromium.launch(
  process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH } : {},
);
let total = 0;

try {
  for (const theme of THEMES) {
    // Reduced motion zeroes out the site's color transitions (see index.css), so
    // axe never samples a color halfway through one
    const ctx = await browser.newContext({
      viewport: { width: 1280, height: 900 },
      reducedMotion: 'reduce',
    });
    await ctx.addInitScript((t) => {
      localStorage.setItem('theme', t);
      localStorage.removeItem('themeState');
    }, theme);
    await ctx.route('**/api/**', (route) => route.fulfill({ json: mockApi(route.request().url()) }));
    await ctx.route('https://assets.nhle.com/**', (route) =>
      route.fulfill({ contentType: 'image/svg+xml', body: LOGO_SVG }),
    );
    await ctx.route('https://fonts.*/**', (route) => route.abort());
    const page = await ctx.newPage();

    for (const path of pagesFor()) {
      await page.goto(BASE + path, { waitUntil: 'networkidle' });
      await page.waitForTimeout(500);
      // Open collapsed sections so their contents get scanned too
      for (const button of await page.locator('main button[aria-expanded="false"]').all()) {
        await button.click({ timeout: 1000 }).catch(() => {});
      }
      await page.waitForTimeout(300);
      await page.addScriptTag({ content: axeSource });
      const violations = await page.evaluate(async (tags) => {
        const result = await window.axe.run(document, { runOnly: tags });
        return result.violations.map((v) => ({
          id: v.id,
          impact: v.impact,
          help: v.helpUrl,
          nodes: v.nodes.map((n) => `${n.target.join(' ')}\n        ${n.failureSummary}`),
        }));
      }, AXE_TAGS);

      const count = violations.reduce((sum, v) => sum + v.nodes.length, 0);
      total += count;
      console.log(`${count === 0 ? 'PASS' : 'FAIL'}  [${theme}] ${path}`);
      for (const v of violations) {
        console.log(`  ${v.id} (${v.impact}) ${v.help}`);
        for (const node of v.nodes) console.log(`    - ${node}`);
      }
    }
    await ctx.close();
  }
} finally {
  await browser.close();
  server.kill();
}

console.log(`\n${total} accessibility violation(s)`);
process.exit(total === 0 ? 0 : 1);
