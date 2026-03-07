const FIRST_SEASON = 2009;

export function getCurrentSeason(date: Date = new Date()): number {
  const year = date.getFullYear();
  const month = date.getMonth(); // 0-indexed
  return month >= 9 ? year : year - 1; // October (9) or later = current year
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
