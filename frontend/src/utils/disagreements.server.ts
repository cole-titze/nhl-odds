// The Disagreements page's data, built on the server from the season's games (about 1.4 MB from
// the API) and kept for a minute per season, so a page render or a /disagreements/data.json request
// sends only the rows the page shows and the season isn't re-ranked for every visitor.
import { getGameOddsInDateRange } from '../api/gameOdds';
import { buildDisagreements, type DisagreementsData } from './marketDisagreements';

const MAX_AGE_MS = 60 * 1000;
const cache = new Map<number, { at: number; data: Promise<DisagreementsData> }>();

export function getDisagreements(season: number, init?: RequestInit): Promise<DisagreementsData> {
  const hit = cache.get(season);
  if (hit && Date.now() - hit.at < MAX_AGE_MS) return hit.data;
  const data = getGameOddsInDateRange(`${season}-09-01`, `${season + 1}-07-31`, season, init).then(
    (games) => buildDisagreements(games),
  );
  cache.set(season, { at: Date.now(), data });
  // A failed build isn't kept, so the next request tries again
  data.catch(() => {
    if (cache.get(season)?.data === data) cache.delete(season);
  });
  return data;
}
