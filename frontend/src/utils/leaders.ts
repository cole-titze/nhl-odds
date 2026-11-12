// The Leaders page's stats (#182), in the order the picker lists them. The keys match the API's
// (WebApi StatLeaders.Stats).
import { FIRST_SEASON, getCurrentSeason } from './season';

export interface LeaderStatOption {
  key: string;
  label: string;
  /** The table's column header */
  short: string;
  group: 'skater' | 'goalie';
  format: (value: number) => string;
}

const whole = (v: number) => v.toLocaleString('en-US');
const signed = (v: number) => (v > 0 ? `+${v}` : v < 0 ? `−${Math.abs(v)}` : '0');
const minutes = (seconds: number) => {
  const s = Math.round(seconds);
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
};

export const LEADER_STATS: LeaderStatOption[] = [
  { key: 'points', label: 'Points', short: 'PTS', group: 'skater', format: whole },
  { key: 'goals', label: 'Goals', short: 'G', group: 'skater', format: whole },
  { key: 'assists', label: 'Assists', short: 'A', group: 'skater', format: whole },
  { key: 'shots', label: 'Shots on goal', short: 'SOG', group: 'skater', format: whole },
  { key: 'plusMinus', label: 'Plus/minus', short: '+/−', group: 'skater', format: signed },
  {
    key: 'powerPlayGoals',
    label: 'Power play goals',
    short: 'PPG',
    group: 'skater',
    format: whole,
  },
  { key: 'penaltyMinutes', label: 'Penalty minutes', short: 'PIM', group: 'skater', format: whole },
  { key: 'hits', label: 'Hits', short: 'HIT', group: 'skater', format: whole },
  { key: 'blockedShots', label: 'Blocked shots', short: 'BLK', group: 'skater', format: whole },
  {
    key: 'timeOnIcePerGame',
    label: 'Time on ice per game',
    short: 'TOI/GP',
    group: 'skater',
    format: minutes,
  },
  {
    key: 'savePercentage',
    label: 'Save percentage',
    short: 'SV%',
    group: 'goalie',
    format: (v) => v.toFixed(3).replace(/^0/, ''),
  },
  {
    key: 'goalsAgainstAverage',
    label: 'Goals against average',
    short: 'GAA',
    group: 'goalie',
    format: (v) => v.toFixed(2),
  },
  { key: 'saves', label: 'Saves', short: 'SV', group: 'goalie', format: whole },
  { key: 'shutouts', label: 'Shutouts', short: 'SO', group: 'goalie', format: whole },
  { key: 'gamesStarted', label: 'Games started', short: 'GS', group: 'goalie', format: whole },
];

export const DEFAULT_LEADER_STAT = 'points';

/** The stat and season in a Leaders URL (?stat=goals&season=2024); unknown values fall back to the defaults */
export function leadersView(searchParams: URLSearchParams): {
  stat: LeaderStatOption;
  season: number;
} {
  const stat =
    LEADER_STATS.find((s) => s.key === searchParams.get('stat')) ??
    LEADER_STATS.find((s) => s.key === DEFAULT_LEADER_STAT)!;
  const asked = Number(searchParams.get('season'));
  const season =
    Number.isInteger(asked) && asked >= FIRST_SEASON && asked <= getCurrentSeason()
      ? asked
      : getCurrentSeason();
  return { stat, season };
}
