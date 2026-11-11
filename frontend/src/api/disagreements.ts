import { reportApiFailure } from '../utils/errorReporting';
import type { DisagreementsData } from '../utils/marketDisagreements';

// In the browser: the rows the Disagreements page shows, built by the frontend server
// (routes/disagreements.data.ts) rather than from the whole season's games
export async function fetchDisagreements(season: number): Promise<DisagreementsData> {
  const path = `/disagreements/data.json?season=${season}`;
  let res: Response;
  try {
    res = await fetch(path);
  } catch (e) {
    reportApiFailure('GET', path, null, e);
    throw e;
  }
  if (!res.ok) {
    reportApiFailure('GET', path, res.status);
    throw new Error(`Couldn't load the disagreements (${res.status})`);
  }
  return res.json();
}
