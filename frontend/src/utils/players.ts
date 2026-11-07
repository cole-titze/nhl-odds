import type { PositionCode } from '../types';

const POSITION_NAMES: Record<PositionCode, string> = {
  C: 'Center',
  L: 'Left wing',
  R: 'Right wing',
  D: 'Defense',
  G: 'Goalie',
};

export const positionName = (code: PositionCode | null) => (code ? POSITION_NAMES[code] : '');

export const playerName = (p: { firstName: string; lastName: string }) =>
  `${p.firstName} ${p.lastName}`;

export const playerPath = (id: number) => `/player/${id}`;

/** Rosters come forwards, then defense, then goalies; this splits them for headings */
export function positionGroups<T extends { position: PositionCode }>(players: T[]) {
  return [
    { label: 'Forwards', players: players.filter((p) => 'CLR'.includes(p.position)) },
    { label: 'Defense', players: players.filter((p) => p.position === 'D') },
    { label: 'Goalies', players: players.filter((p) => p.position === 'G') },
  ].filter((g) => g.players.length > 0);
}

/** Time on ice as minutes:seconds */
export function formatToi(seconds: number): string {
  const s = Math.max(0, Math.round(seconds));
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}

/** .915 style, as hockey writes save percentage */
export function formatSavePct(pct: number | null): string {
  if (pct == null) return '-';
  return pct.toFixed(3).replace(/^0/, '');
}

export const formatPlusMinus = (n: number) => (n > 0 ? `+${n}` : String(n));

export function formatHeight(inches: number): string {
  return inches > 0 ? `${Math.floor(inches / 12)}′${inches % 12}″` : '';
}

const CENTRAL_DAY = new Intl.DateTimeFormat('en-CA', {
  timeZone: 'America/Chicago',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

/** A UTC game time's day in Central time (yyyy-MM-dd), the day the Games feed lists it under */
export function centralDay(utc: string): string {
  const iso = /(?:[zZ]|[+-]\d\d:\d\d)$/.test(utc) ? utc : `${utc}Z`;
  return CENTRAL_DAY.format(new Date(iso));
}
