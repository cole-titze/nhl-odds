// Builds /sitemap.xml from src/sitePages.ts, the season's teams (#156) and the
// player pages (#171, utils/playerSitemap.ts: each dated their last game).
// <lastmod> is the latest game day so far (Central time, like the Games feed):
// for 'daily' pages any team's, for a team page that team's. Days after today
// don't count, so a schedule of upcoming games doesn't move it.
import type { TeamVM } from '../types';
import { LISTED_PATHS } from '../sitePages';
import { SITE_URL } from './site';
import { gameDateBucket } from './dates';

type SitemapTeam = Pick<TeamVM, 'id' | 'gameOddsVM'>;

function latestDay(gameDates: string[], today: string): string | undefined {
  let latest: string | undefined;
  for (const d of gameDates) {
    const day = gameDateBucket(d);
    if (day <= today && (!latest || day > latest)) latest = day;
  }
  return latest;
}

const url = (path: string, lastmod?: string) =>
  `<url><loc>${SITE_URL}${path}</loc>${lastmod ? `<lastmod>${lastmod}</lastmod>` : ''}</url>`;

/**
 * One sitemap file holds at most 50,000 URLs (and 50 MB). Players since 2009 are a few thousand, so one file
 * is plenty. Past SITEMAP_URL_WARNING the server logs an error each time it builds the sitemap, well before the
 * limit, so there's time to split it into a sitemap index (pages, teams, players).
 */
const SITEMAP_URL_LIMIT = 50_000;
const SITEMAP_URL_WARNING = 40_000;

export function buildSitemapXml(
  teams: SitemapTeam[],
  players: { path: string; lastmod: string }[] = [],
  now = new Date(),
): string {
  const today = gameDateBucket(now.toISOString());
  const teamDays = teams.map((t) => ({
    id: t.id,
    day: latestDay(
      (t.gameOddsVM ?? []).map((g) => g.gameDate),
      today,
    ),
  }));
  const siteDay = teamDays.reduce<string | undefined>(
    (max, t) => (t.day && (!max || t.day > max) ? t.day : max),
    undefined,
  );
  const urls = [
    ...LISTED_PATHS.map(({ path, daily }) => url(path, daily ? siteDay : undefined)),
    ...teamDays.sort((a, b) => a.id - b.id).map((t) => url(`/team/${t.id}`, t.day)),
    ...players.map((p) => url(p.path, p.lastmod)),
  ];
  if (urls.length > SITEMAP_URL_WARNING) {
    console.error(
      `sitemap.xml has ${urls.length} URLs; one file holds ${SITEMAP_URL_LIMIT}: split it into a sitemap index`,
    );
  }
  return [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
    ...urls,
    '</urlset>',
  ].join('\n');
}
