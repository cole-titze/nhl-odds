// Fails the build when a route in src/routes.ts isn't in src/sitePages.ts
// (which decides whether it goes in /sitemap.xml), when sitePages lists a route
// that no longer exists, or when the a11y scan (a11y/fixtures.mjs pagesFor)
// visits no page of a route. Runs first in `npm run build`.
import { readFileSync } from 'node:fs';
import { pagesFor } from '../a11y/fixtures.mjs';
import { SITE_PAGES } from '../src/sitePages.ts';

const routesSource = readFileSync(new URL('../src/routes.ts', import.meta.url), 'utf8');
// routes.ts is flat (one layout, no nested paths): index('…') is '/', route('path', …) is '/path'
const routes = [
  ...(/\bindex\(/.test(routesSource) ? ['/'] : []),
  ...[...routesSource.matchAll(/\broute\(\s*'([^']*)'/g)].map((m) => `/${m[1]}`),
];

const problems = [];
for (const r of routes) {
  if (!(r in SITE_PAGES)) {
    problems.push(
      `${r} is in src/routes.ts but not in src/sitePages.ts: add it there, as 'static' to list it in the sitemap or { exclude: '<reason>' } to leave it out`,
    );
  }
}
for (const p of Object.keys(SITE_PAGES)) {
  if (!routes.includes(p)) problems.push(`${p} is in src/sitePages.ts but not in src/routes.ts`);
}

// Pages (not files such as the sitemap or the /og/ link preview images) need an a11y scan
const scanned = pagesFor();
const matches = (route, path) =>
  new RegExp(`^${route.replace(/:[^/]+/g, '[^/]+')}$`).test(path.split('?')[0]);
for (const r of routes.filter((r) => !/\.\w+$/.test(r) && !r.startsWith('/og/'))) {
  if (!scanned.some((p) => matches(r, p))) {
    problems.push(
      `${r} has no page in the a11y scan: add one to pagesFor() in a11y/fixtures.mjs (and mock any API calls it makes)`,
    );
  }
}

if (problems.length) {
  for (const p of problems) console.error(`check-site-pages: ${p}`);
  process.exit(1);
}
console.log(`check-site-pages: ${routes.length} routes OK`);
