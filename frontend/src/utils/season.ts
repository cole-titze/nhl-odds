export const FIRST_SEASON = 2009;

export function getCurrentSeason(date: Date = new Date()): number {
  const year = date.getFullYear();
  const seasonStart = new Date(year, 8, 20); // September 20
  return date >= seasonStart ? year : year - 1;
}

export function getSeasonOptions(): number[] {
  const current = getCurrentSeason();
  const seasons: number[] = [];
  for (let y = current; y >= FIRST_SEASON; y--) {
    seasons.push(y);
  }
  return seasons;
}

export function formatSeasonLabel(year: number): string {
  return `${year}-${String(year + 1).slice(2)}`;
}
