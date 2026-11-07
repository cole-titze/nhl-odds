// Player pages in /sitemap.xml (#171): every player with a page and a game,
// retired ones included, each with <lastmod> the day of their last game
// (Central time, like the Games feed). A retired player's date never moves, so
// search engines can tell the page is stable. Only real past dates go in: a
// missing, unreadable or future date leaves the player out rather than getting
// the build time (players with no game never come from the API at all).
import type { ActivePlayerVM } from '../types';
import { centralDay, playerPath } from './players';

export function playerSitemapEntries(players: ActivePlayerVM[], now = new Date()) {
  const today = centralDay(now.toISOString());
  return players
    .filter((p) => p.lastGameDate && !Number.isNaN(Date.parse(p.lastGameDate)))
    .map((p) => ({ id: p.id, path: playerPath(p.id), lastmod: centralDay(p.lastGameDate) }))
    .filter((p) => p.lastmod <= today)
    .sort((a, b) => a.id - b.id)
    .map(({ path, lastmod }) => ({ path, lastmod }));
}
