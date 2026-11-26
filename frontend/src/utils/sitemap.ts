// /sitemap.xml is a sitemap index: /sitemap.xml?part=pages (src/sitePages.ts, the
// season's teams (#156) and the player pages (#171, utils/playerSitemap.ts: each
// dated their last game)) and one /sitemap.xml?games=<season> per season (its game
// pages, and for past seasons its team pages). The files stay at /sitemap.xml, with
// a query, because a sitemap may only list URLs under its own directory.
// <lastmod> is the latest game day so far (Central time, like the Games feed):
// for 'daily' pages any team's, for a team page that team's. Days after today
// don't count, so a schedule of upcoming games doesn't move it.
import type { SitemapGameVM, TeamVM } from '../types';
import { LISTED_PATHS } from '../sitePages';
import { SITE_URL } from './site';
import { centralDay, gameDateBucket } from './dates';
import { gamePath, teamPath } from './pagePaths';
import { getCurrentSeason } from './season';

type SitemapTeam = Pick<TeamVM, 'id' | 'gameOddsVM'>;

function latestDay(gameDates: string[], today: string): string | undefined {
  let latest: string | undefined;
  for (const d of gameDates) {
    const day = gameDateBucket(d);
    if (day <= today && (!latest || day > latest)) latest = day;
  }
  return latest;
}

const xmlText = (s: string) =>
  s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

const url = (path: string, lastmod?: string) =>
  `<url><loc>${xmlText(SITE_URL + path)}</loc>${lastmod ? `<lastmod>${lastmod}</lastmod>` : ''}</url>`;

const urlset = (urls: string[]) =>
  [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
    ...urls,
    '</urlset>',
  ].join('\n');

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
  return urlset(urls);
}

/** Upcoming games are listed this many days ahead (their prediction comes the morning of) */
const UPCOMING_DAYS = 7;

const latest = (days: (string | undefined)[]) =>
  days.reduce<string | undefined>((max, d) => (d && (!max || d > max) ? d : max), undefined);

/** A game's Central-time day once played (its lastmod), else undefined */
const playedDay = (g: SitemapGameVM, today: string) => {
  const day = centralDay(g.gameDate);
  return g.hasBeenPlayed && day <= today ? day : undefined;
};

/** The games the sitemap lists, by season: played ones and the next UPCOMING_DAYS' */
function listedGames(games: SitemapGameVM[], now: Date) {
  const today = gameDateBucket(now.toISOString());
  const until = gameDateBucket(new Date(now.getTime() + UPCOMING_DAYS * 86_400_000).toISOString());
  const bySeason = new Map<number, SitemapGameVM[]>();
  for (const g of games) {
    const day = centralDay(g.gameDate);
    if (playedDay(g, today) || (!g.hasBeenPlayed && day >= today && day <= until)) {
      const list = bySeason.get(g.seasonStartYear) ?? [];
      list.push(g);
      bySeason.set(g.seasonStartYear, list);
    }
  }
  return bySeason;
}

/**
 * One season's file: its played games, dated their day, and games in the next UPCOMING_DAYS
 * (no lastmod: no real date yet); for a past season, also each team's page for it, dated its
 * last game. This season's team pages are in the pages file.
 */
export function buildGamesSitemapXml(
  season: number,
  allGames: SitemapGameVM[],
  now = new Date(),
): string | null {
  const today = gameDateBucket(now.toISOString());
  const games = listedGames(allGames, now).get(season);
  if (!games) return null;
  const urls = games.map((g) =>
    url(gamePath(g.id, g.awayTeamName, g.homeTeamName, g.gameDate), playedDay(g, today)),
  );
  if (season !== getCurrentSeason()) {
    const teamDays = new Map<number, string | undefined>();
    for (const g of games)
      for (const id of [g.awayTeamId, g.homeTeamId])
        teamDays.set(id, latest([teamDays.get(id), playedDay(g, today)]));
    for (const [id, day] of [...teamDays].sort((a, b) => a[0] - b[0]))
      urls.push(url(teamPath(id, season), day));
  }
  return urlset(urls);
}

/** /sitemap.xml: the pages file, then each season's games file (newest first), dated their latest game */
export function buildSitemapIndexXml(games: SitemapGameVM[], now = new Date()): string {
  const today = gameDateBucket(now.toISOString());
  // Only seasons with a file (one whose games are all weeks away has none yet)
  const seasonDays = new Map(
    [...listedGames(games, now)].map(([season, list]) => [
      season,
      latest(list.map((g) => playedDay(g, today))),
    ]),
  );
  const sitemap = (query: string, lastmod?: string) =>
    `<sitemap><loc>${xmlText(`${SITE_URL}/sitemap.xml?${query}`)}</loc>${lastmod ? `<lastmod>${lastmod}</lastmod>` : ''}</sitemap>`;
  return [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
    sitemap('part=pages', latest([...seasonDays.values()])),
    ...[...seasonDays]
      .sort((a, b) => b[0] - a[0])
      .map(([season, day]) => sitemap(`games=${season}`, day)),
    '</sitemapindex>',
  ].join('\n');
}
