// /sitemap.xml: every page search engines should index, so they can find
// game and team pages without crawling the whole feed. Covers this season and
// last; older seasons are still reachable through links.
import { getAllTeams } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import { SITE_URL } from '../utils/site';

const STATIC_PATHS = ['/', '/teams', '/strategies', '/about'];

// Building it takes two full-season API calls, so it's reused for an hour
const MAX_AGE_MS = 60 * 60 * 1000;
let cached: { xml: string; at: number } | null = null;

async function buildSitemap(): Promise<string> {
  const season = getCurrentSeason();
  const seasons = await Promise.all([getAllTeams(season), getAllTeams(season - 1)]);
  const teamIds = new Set<number>();
  const gameIds = new Set<number>();
  for (const { teams } of seasons) {
    for (const team of teams) {
      teamIds.add(team.id);
      for (const game of team.gameOddsVM) gameIds.add(game.id);
    }
  }
  const paths = [
    ...STATIC_PATHS,
    ...[...teamIds].sort((a, b) => a - b).map((id) => `/team/${id}`),
    ...[...gameIds].sort((a, b) => b - a).map((id) => `/game/${id}`),
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
