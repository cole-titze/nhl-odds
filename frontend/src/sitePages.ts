// Every route in routes.ts, and whether it goes in /sitemap.xml. A new route
// must be added here too: `npm run build` runs scripts/check-site-pages.mjs,
// which fails while a route is missing (and checks the a11y scan covers it).
//
// Plain data (no imports), so that script can load it with Node.
//
// <lastmod> is only given where it's a real date (search engines stop trusting a
// sitemap whose dates are just when it was built).
export type SitemapEntry =
  /** Listed, lastmod the latest game day so far: the page changes with each day's games */
  | 'daily'
  /** Listed without lastmod: the content only changes when the site does */
  | 'static'
  /** One URL per team, each with lastmod its latest game day so far, from the teams API */
  | 'teams'
  /** One URL per player with a game, dated their last game (utils/playerSitemap.ts) */
  | 'players'
  /** One URL per game, played (dated its day) or in the next week, in its season's file (utils/sitemap.ts) */
  | 'games'
  /** Left out, with the reason */
  | { exclude: string };

export const SITE_PAGES: Record<string, SitemapEntry> = {
  '/': 'daily',
  '/strategies': 'daily',
  '/leaders': 'daily',
  '/teams': 'daily',
  '/track-record': 'daily',
  '/about': 'static',
  '/team/:teamId': 'teams',
  '/player/:playerId/:slug?': 'players',
  '/game/:gameId/:slug?': 'games',
  '/admin': { exclude: 'noindex: needs an admin key' },
  '/search': { exclude: 'noindex: search results for whatever was typed' },
  '/sitemap.xml': { exclude: 'the sitemap index and its files themselves' },
  '/og/game/:file': { exclude: 'link preview image (og:image), not a page' },
  '/og/team/:file': { exclude: 'link preview image (og:image), not a page' },
  '/og/:file': { exclude: 'link preview image (og:image), not a page' },
};

/** The paths the sitemap lists as they are, and whether their lastmod is the latest game day */
export const LISTED_PATHS = Object.entries(SITE_PAGES).flatMap(([path, entry]) =>
  entry === 'static' || entry === 'daily' ? [{ path, daily: entry === 'daily' }] : [],
);
