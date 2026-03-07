import { Winner } from '../types';
import type { GameOddsVM } from '../types';

export function wasCorrectlyPredicted(game: GameOddsVM): boolean {
  if (!game.hasBeenPlayed || !game.homeTeam || !game.awayTeam) return false;
  const homeOdds = game.homeTeam.modelOdds;
  const predictedHome = homeOdds >= 0.5;
  const actualHome = game.winner === Winner.HOME;
  return predictedHome === actualHome;
}

export function predictionBorderClass(game: GameOddsVM): string {
  if (!game.hasBeenPlayed) {
    return 'border-blue-400 dark:border-blue-600';
  }
  return wasCorrectlyPredicted(game)
    ? 'border-green-500 bg-green-50 dark:bg-green-950'
    : 'border-red-500 bg-red-50 dark:bg-red-950';
}
