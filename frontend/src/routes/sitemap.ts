// /sitemap.xml: the main pages and every team page. Game pages are left out on
// purpose: thousands of near-identical pages add little, and search engines
// can still reach them through links.
import { getAllTeams } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import { SITE_URL } from '../utils/site';

const STATIC_PATHS = ['/', '/strategies', '/teams', '/about'];

// Building it takes a full-season API call, so it's reused for an hour
const MAX_AGE_MS = 60 * 60 * 1000;
let cached: { xml: string; at: number } | null = null;

async function buildSitemap(): Promise<string> {
  const { teams } = await getAllTeams(getCurrentSeason());
  const paths = [
    ...STATIC_PATHS,
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
