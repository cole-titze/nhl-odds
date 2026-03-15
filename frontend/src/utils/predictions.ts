import { Winner } from '../types';
import type { GameOddsVM } from '../types';

export function wasCorrectlyPredicted(game: GameOddsVM): boolean {
  if (!game.hasBeenPlayed || !game.homeTeam || !game.awayTeam) return false;
  const homeOdds = game.homeTeam.modelOdds;
  const predictedHome = homeOdds >= 0.5;
  const actualHome = game.winner === Winner.HOME;
  return predictedHome === actualHome;
}

export function calculateLogLoss(homeOdds: number, awayOdds: number, winner: Winner): number {
  if (homeOdds <= 0 || awayOdds <= 0) return -1;
  const w = winner as number;
  return -(w * Math.log(awayOdds) + (1 - w) * Math.log(homeOdds));
}

export function predictionBorderClass(game: GameOddsVM): string {
  if (!game.hasBeenPlayed) {
    return 'border-surface-200 dark:border-white/[0.06]';
  }
  return wasCorrectlyPredicted(game)
    ? 'border-emerald-500/40 dark:border-emerald-500/20 !bg-emerald-50/30 dark:!bg-emerald-500/[0.03]'
    : 'border-red-500/40 dark:border-red-500/20 !bg-red-50/30 dark:!bg-red-500/[0.03]';
}
