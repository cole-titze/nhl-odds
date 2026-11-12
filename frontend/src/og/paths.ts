// Which share image (served by routes/og.*.tsx) each page's link preview shows.
// Client-safe: PageTitle reads it on every page.

/** Image path for a page path; pages without one of their own get the site card. */
export function shareImagePath(pathname: string): string {
  const path = pathname.replace(/\/+$/, '') || '/';
  if (path === '/') return '/og/games.png';
  const game = path.match(/^\/game\/(\d+)$/);
  if (game) return `/og/game/${game[1]}.png`;
  const team = path.match(/^\/team\/(\d+)$/);
  if (team) return `/og/team/${team[1]}.png`;
  if (path === '/teams' || path === '/strategies' || path === '/about' || path === '/leaders')
    return `/og${path}.png`;
  return '/og/site.png';
}

/** The id in an image file name ("2026020100.png"), or null if it isn't one. */
export function imageId(file: string | undefined): number | null {
  const m = file?.match(/^(\d{1,10})\.png$/);
  return m ? Number(m[1]) : null;
}
