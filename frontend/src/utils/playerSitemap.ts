// Player pages in /sitemap.xml (#169): only players with a game this season,
// each with <lastmod> the day of his last game (Central time, like the Games
// feed): a real date the page last changed, never when the sitemap was built.
// Players without games this season still have pages, reached through links.
import type { ActivePlayerVM } from '../types';
import { centralDay, playerPath } from './players';

export function playerSitemapEntries(players: ActivePlayerVM[]) {
  return [...players]
    .sort((a, b) => a.id - b.id)
    .map((p) => ({ path: playerPath(p.id), lastmod: centralDay(p.lastGameDate) }));
}
