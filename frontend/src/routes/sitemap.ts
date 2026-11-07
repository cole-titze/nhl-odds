// /sitemap.xml: the pages src/sitePages.ts marks for it (the main pages and
// every team page). New pages are added there, not here.
import { getAllTeams } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import { SITE_URL } from '../utils/site';
import { STATIC_SITEMAP_PATHS } from '../sitePages';

// Building it takes a full-season API call, so it's reused for an hour
const MAX_AGE_MS = 60 * 60 * 1000;
let cached: { xml: string; at: number } | null = null;

async function buildSitemap(): Promise<string> {
  const { teams } = await getAllTeams(getCurrentSeason());
  const paths = [
    ...STATIC_SITEMAP_PATHS,
    ...teams
      .map((t) => t.id)
      .sort((a, b) => a - b)
      .map((id) => `/team/${id}`),
  ];
  return [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
    ...paths.map((p) => `<url><loc>${SITE_URL}${p}</loc></url>`),
    '</urlset>',
  ].join('\n');
}

export async function loader() {
  if (!cached || Date.now() - cached.at > MAX_AGE_MS) {
    cached = { xml: await buildSitemap(), at: Date.now() };
  }
  return new Response(cached.xml, {
    headers: {
      'Content-Type': 'application/xml; charset=utf-8',
      'Cache-Control': 'public, max-age=3600',
    },
  });
}
