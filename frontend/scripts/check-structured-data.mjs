// Checks every page's schema.org JSON-LD (#167): renders each page in the a11y
// scan's list on the production server (with its mocked API, so loaders run and
// pages render as crawlers get them) and fails if a page's JSON-LD is missing,
// isn't valid JSON, lacks the site-wide items, or lacks its page type's items,
// or if the About page's FAQ markup doesn't match its visible questions.
// Teams, players and games must carry their page's URL as `@id` (#169), so the
// markup on different pages points at the same entities.
// It also checks /sitemap.xml (#171): every player page is listed with a real,
// past <lastmod>, retired players included, and bad dates are left out.
//
//   npm run build && npm run check-schema
import { spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { SITEMAP_LEFT_OUT, SITEMAP_RETIRED, mockApi, pagesFor } from '../a11y/fixtures.mjs';

const PORT = 4183;
const API_PORT = 4184;
const SITE_WIDE = ['WebSite', 'Organization', 'WebPage'];
// Types each kind of page must have besides SITE_WIDE (first matching prefix)
const PAGE_TYPES = [
  ['/game/', ['SportsEvent', 'BreadcrumbList']],
  ['/player/', ['Person', 'BreadcrumbList']],
  ['/team/', ['SportsTeam', 'BreadcrumbList']],
  ['/teams', ['ItemList']],
  ['/about', ['FAQPage']],
];
const NOINDEX = ['/admin'];
const SITE_URL = 'https://nhl.half-built.com';
const pageUrl = (path) => SITE_URL + path;
const isEntityUrl = (id, kind) => typeof id === 'string' && new RegExp(`^${SITE_URL}/${kind}/\\d+$`).test(id);

// The entity links (#169): each page's main entity has its URL as @id and
// links to the others by theirs
function checkEntities(path, graph, problems) {
  const find = (type) => graph.find((item) => item['@type'] === type);
  if (path.startsWith('/player/')) {
    const person = find('Person');
    if (person?.['@id'] !== pageUrl(path)) problems.push(`${path}: Person @id isn't the page URL`);
    if (!isEntityUrl(person?.memberOf?.['@id'], 'team'))
      problems.push(`${path}: Person has no memberOf SportsTeam with a team page @id`);
  } else if (path.startsWith('/team/')) {
    const team = find('SportsTeam');
    if (team?.['@id'] !== pageUrl(path)) problems.push(`${path}: SportsTeam @id isn't the page URL`);
    const athletes = team?.athlete ?? [];
    if (!athletes.length) problems.push(`${path}: SportsTeam lists no athletes`);
    for (const a of athletes)
      if (a['@type'] !== 'Person' || !isEntityUrl(a['@id'], 'player'))
        problems.push(`${path}: athlete ${a.name} isn't a Person with a player page @id`);
  } else if (path.startsWith('/game/')) {
    const event = find('SportsEvent');
    if (event?.['@id'] !== pageUrl(path)) problems.push(`${path}: SportsEvent @id isn't the page URL`);
    const competitors = (event?.competitor ?? []).map((c) => c['@id']);
    const teams = [event?.homeTeam?.['@id'], event?.awayTeam?.['@id']];
    if (competitors.length !== 2 || !teams.every((t) => isEntityUrl(t, 'team') && competitors.includes(t)))
      problems.push(`${path}: SportsEvent competitors aren't its two teams by team page @id`);
  }
}

const decode = (s) =>
  s
    .replace(/&#x27;|&#39;/g, "'")
    .replace(/&quot;/g, '"')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&amp;/g, '&');

async function waitForServer() {
  for (let i = 0; i < 60; i++) {
    try {
      if ((await fetch(`http://localhost:${PORT}/healthz`)).ok) return;
    } catch {
      /* not up yet */
    }
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error('server.mjs did not start; did you run `npm run build`?');
}

async function checkPage(path, problems) {
  const html = await (await fetch(`http://localhost:${PORT}${path}`)).text();
  const blocks = [...html.matchAll(/<script type="application\/ld\+json">([\s\S]*?)<\/script>/g)];
  if (NOINDEX.includes(path)) {
    if (blocks.length) problems.push(`${path}: noindex page has JSON-LD`);
    return;
  }
  if (blocks.length !== 1) {
    problems.push(`${path}: expected 1 JSON-LD block, found ${blocks.length}`);
    return;
  }
  let data;
  try {
    data = JSON.parse(blocks[0][1]);
  } catch (e) {
    problems.push(`${path}: JSON-LD is not valid JSON (${e.message})`);
    return;
  }
  const graph = data['@graph'] ?? [];
  const types = graph.map((item) => item['@type']);
  if (data['@context'] !== 'https://schema.org') problems.push(`${path}: @context is not https://schema.org`);
  const required = [...SITE_WIDE, ...(PAGE_TYPES.find(([p]) => path.startsWith(p))?.[1] ?? [])];
  for (const t of required) if (!types.includes(t)) problems.push(`${path}: no ${t} in JSON-LD`);
  if (graph.some((item) => !item['@type'])) problems.push(`${path}: a JSON-LD item has no @type`);
  checkEntities(path, graph, problems);

  const faq = graph.find((item) => item['@type'] === 'FAQPage');
  if (faq) {
    const page = decode(html);
    const visible = new Set([...html.matchAll(/<h3[^>]*>([^<]+)<\/h3>/g)].map((m) => decode(m[1])));
    for (const q of faq.mainEntity) {
      if (!visible.has(q.name)) problems.push(`${path}: FAQ question "${q.name}" isn't on the page`);
      if (!page.includes(q.acceptedAnswer.text)) problems.push(`${path}: FAQ answer to "${q.name}" isn't on the page`);
    }
  }
  console.log(`${path}: ${types.join(', ')}`);
}

// One sitemap file holds 50,000 URLs and 50 MB (see SITEMAP_URL_LIMIT in routes/sitemap.ts)
const SITEMAP_MAX_URLS = 50_000;
const SITEMAP_MAX_BYTES = 50 * 1024 * 1024;

async function checkSitemap(problems) {
  const res = await fetch(`http://localhost:${PORT}/sitemap.xml`);
  const xml = await res.text();
  if (!res.ok) {
    problems.push(`/sitemap.xml: HTTP ${res.status}`);
    return;
  }
  const entries = [...xml.matchAll(/<url><loc>([^<]+)<\/loc>(?:<lastmod>([^<]+)<\/lastmod>)?<\/url>/g)].map(
    ([, loc, lastmod]) => ({ loc, lastmod }),
  );
  const urlCount = (xml.match(/<url>/g) ?? []).length;
  if (entries.length !== urlCount) problems.push(`/sitemap.xml: ${urlCount - entries.length} <url> entries didn't parse`);
  if (urlCount > SITEMAP_MAX_URLS) problems.push(`/sitemap.xml: ${urlCount} URLs, over ${SITEMAP_MAX_URLS}`);
  if (Buffer.byteLength(xml) > SITEMAP_MAX_BYTES) problems.push('/sitemap.xml: over 50 MB');
  if (/<changefreq>|<priority>/.test(xml)) problems.push('/sitemap.xml: has changefreq or priority');

  // A day of slack: lastmod is a Central-time day, and this is UTC's
  const tomorrow = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10);
  const players = entries.filter((e) => e.loc.startsWith(`${SITE_URL}/player/`));
  for (const { loc, lastmod } of players) {
    const real = /^\d{4}-\d{2}-\d{2}$/.test(lastmod ?? '') && !Number.isNaN(Date.parse(lastmod));
    if (!real) problems.push(`/sitemap.xml: ${loc} has no real <lastmod> (${lastmod})`);
    else if (lastmod > tomorrow) problems.push(`/sitemap.xml: ${loc} <lastmod> ${lastmod} is in the future`);
  }
  const listed = new Map(players.map((e) => [Number(e.loc.split('/').pop()), e.lastmod]));
  const expected = mockApi('http://localhost/api/Player/GetSitemapPlayers').length - SITEMAP_LEFT_OUT.length;
  if (listed.size !== expected) problems.push(`/sitemap.xml: ${listed.size} player pages, expected ${expected}`);
  if (listed.get(SITEMAP_RETIRED.id) !== SITEMAP_RETIRED.lastmod)
    problems.push(`/sitemap.xml: retired player ${SITEMAP_RETIRED.id} isn't dated ${SITEMAP_RETIRED.lastmod}`);
  for (const id of SITEMAP_LEFT_OUT)
    if (listed.has(id)) problems.push(`/sitemap.xml: player ${id} (future or bad date) is listed`);
  console.log(`/sitemap.xml: ${urlCount} URLs, ${players.length} player pages`);
}

const api = createServer((req, res) => {
  res.setHeader('Content-Type', 'application/json');
  res.end(JSON.stringify(mockApi(`http://localhost${req.url}`)));
}).listen(API_PORT);
const server = spawn(process.execPath, ['server.mjs'], {
  stdio: ['ignore', 'ignore', 'inherit'],
  env: { ...process.env, PORT: String(PORT), API_URL: `http://127.0.0.1:${API_PORT}` },
});

try {
  await waitForServer();
  const problems = [];
  for (const path of pagesFor()) await checkPage(path, problems);
  await checkSitemap(problems);
  for (const p of problems) console.error(`FAIL: ${p}`);
  process.exitCode = problems.length ? 1 : 0;
} finally {
  server.kill();
  api.close();
}
