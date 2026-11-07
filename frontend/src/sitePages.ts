// Every route in routes.ts, and whether it goes in /sitemap.xml. A new route
// must be added here too: `npm run build` runs scripts/check-site-pages.mjs,
// which fails while a route is missing (and checks the a11y scan covers it).
//
// Plain data (no imports), so that script can load it with Node.
export type SitemapEntry =
  /** Listed as is */
  | 'static'
  /** One URL per team, from the teams API */
  | 'teams'
  /** Left out, with the reason */
  | { exclude: string };

export const SITE_PAGES: Record<string, SitemapEntry> = {
  '/': 'static',
  '/strategies': 'static',
  '/disagreements': 'static',
  '/teams': 'static',
  '/track-record': 'static',
  '/about': 'static',
  '/team/:teamId': 'teams',
  '/game/:gameId': {
    exclude:
      'thousands of near-identical pages add little; search engines still reach them through links',
  },
  '/admin': { exclude: 'noindex: needs an admin key' },
  '/sitemap.xml': { exclude: 'the sitemap itself' },
  '/og/game/:file': { exclude: 'link preview image (og:image), not a page' },
  '/og/team/:file': { exclude: 'link preview image (og:image), not a page' },
  '/og/:file': { exclude: 'link preview image (og:image), not a page' },
};

/** The paths the sitemap lists as they are */
export const STATIC_SITEMAP_PATHS = Object.entries(SITE_PAGES)
  .filter(([, entry]) => entry === 'static')
  .map(([path]) => path);
