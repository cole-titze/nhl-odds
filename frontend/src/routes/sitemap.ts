// /sitemap.xml: a sitemap index (utils/sitemap.ts) of
//   /sitemap.xml?part=pages    the pages src/sitePages.ts marks for it (the main pages, this
//                              season's team pages and every player page with a game)
//   /sitemap.xml?games=<year>  that season's game pages (and a past season's team pages)
// with lastmod where it's a real date. New pages are added in sitePages.ts, not here.
import type { LoaderFunctionArgs } from 'react-router';
import { getAllTeams } from '../api/teams';
import { getSitemapPlayers } from '../api/players';
import { getSitemapGames } from '../api/gameOdds';
import { FIRST_SEASON, getCurrentSeason } from '../utils/season';
import { buildGamesSitemapXml, buildSitemapIndexXml, buildSitemapXml } from '../utils/sitemap';
import { playerSitemapEntries } from '../utils/playerSitemap';
import type { SitemapGameVM } from '../types';

// Each file is built from a few API calls, so it's reused for an hour
const MAX_AGE_MS = 60 * 60 * 1000;
const cache = new Map<string, { value: unknown; at: number }>();

async function cached<T>(key: string, build: () => Promise<T>): Promise<T> {
  const hit = cache.get(key);
  if (hit && Date.now() - hit.at < MAX_AGE_MS) return hit.value as T;
  const value = await build();
  cache.set(key, { value, at: Date.now() });
  return value;
}

// Without the players the pages file still lists the rest
async function sitemapPlayers() {
  try {
    return await getSitemapPlayers();
  } catch {
    return [];
  }
}

async function buildPages(): Promise<string> {
  const [{ teams }, players] = await Promise.all([
    getAllTeams(getCurrentSeason()),
    sitemapPlayers(),
  ]);
  return buildSitemapXml(teams, playerSitemapEntries(players));
}

const games = () => cached<SitemapGameVM[]>('games', getSitemapGames);

const xml = (body: string) =>
  new Response(body, {
    headers: {
      'Content-Type': 'application/xml; charset=utf-8',
      'Cache-Control': 'public, max-age=3600',
    },
  });

const notFound = () => new Response('Not found', { status: 404 });

export async function loader({ request }: LoaderFunctionArgs) {
  const params = new URL(request.url).searchParams;
  const keys = [...params.keys()];
  try {
    if (keys.length === 0)
      return xml(await cached('index', async () => buildSitemapIndexXml(await games())));
    if (keys.length !== 1) return notFound();
    if (params.get('part') === 'pages') return xml(await cached('pages', buildPages));
    const season = params.get('games');
    if (
      season &&
      /^\d{4}$/.test(season) &&
      +season >= FIRST_SEASON &&
      +season <= getCurrentSeason()
    ) {
      const body = await cached(`games-${season}`, async () =>
        buildGamesSitemapXml(Number(season), await games()),
      );
      return body ? xml(body) : notFound();
    }
    return notFound();
  } catch {
    // The API is down: crawlers come back later rather than dropping the files' URLs
    return new Response('Unavailable', { status: 503, headers: { 'Retry-After': '600' } });
  }
}
