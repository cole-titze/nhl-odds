// /sitemap.xml: the pages src/sitePages.ts marks for it (the main pages, every
// team page and every player page with a game), with lastmod where it's a real
// date (utils/sitemap.ts). Game pages are left out on purpose.
// New pages are added in sitePages.ts, not here.
import { getAllTeams } from '../api/teams';
import { getSitemapPlayers } from '../api/players';
import { getCurrentSeason } from '../utils/season';
import { buildSitemapXml } from '../utils/sitemap';
import { playerSitemapEntries } from '../utils/playerSitemap';

// Building it takes two API calls, so it's reused for an hour
const MAX_AGE_MS = 60 * 60 * 1000;
let cached: { xml: string; at: number } | null = null;

// Without the players the sitemap still lists the rest
async function sitemapPlayers() {
  try {
    return await getSitemapPlayers();
  } catch {
    return [];
  }
}

async function buildSitemap(): Promise<string> {
  const [{ teams }, players] = await Promise.all([
    getAllTeams(getCurrentSeason()),
    sitemapPlayers(),
  ]);
  return buildSitemapXml(teams, playerSitemapEntries(players));
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
