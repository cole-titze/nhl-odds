// /sitemap.xml: the pages src/sitePages.ts marks for it (the main pages and
// every team page), with lastmod where it's a real date (utils/sitemap.ts).
// New pages are added in sitePages.ts, not here.
import { getAllTeams } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import { buildSitemapXml } from '../utils/sitemap';

// Building it takes a full-season API call, so it's reused for an hour
const MAX_AGE_MS = 60 * 60 * 1000;
let cached: { xml: string; at: number } | null = null;

async function buildSitemap(): Promise<string> {
  const { teams } = await getAllTeams(getCurrentSeason());
  return buildSitemapXml(teams);
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
