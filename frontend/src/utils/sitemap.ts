// Builds /sitemap.xml from src/sitePages.ts and the season's teams (#156).
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

export function buildSitemapXml(teams: SitemapTeam[], now = new Date()): string {
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
  return [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
    ...LISTED_PATHS.map(({ path, daily }) => url(path, daily ? siteDay : undefined)),
    ...teamDays.sort((a, b) => a.id - b.id).map((t) => url(`/team/${t.id}`, t.day)),
    '</urlset>',
  ].join('\n');
}
