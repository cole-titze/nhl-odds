import { useEffect, useState } from 'react';
import { getPredictionFactors } from '../api/gameOdds';
import type { GameOddsVM, PredictionFactorsVM } from '../types';

/**
 * A game's prediction factors (#155): what the server loaded with the page (null: none saved),
 * or, with `initial` undefined (client navigation), fetched here. The Game page calls it rather
 * than its factors section, so its chips know whether there's one.
 */
export function usePredictionFactors(gameId: number, initial?: PredictionFactorsVM | null) {
  // Kept with the game they're for, so a newly opened game never shows the last one's
  const [fetched, setFetched] = useState<{ gameId: number; data: PredictionFactorsVM | null }>();

  useEffect(() => {
    if (initial !== undefined) return;
    let current = true;
    getPredictionFactors(gameId)
      .then((data) => current && setFetched({ gameId, data }))
      .catch(() => current && setFetched({ gameId, data: null }));
    return () => {
      current = false;
    };
  }, [gameId, initial]);

  if (initial !== undefined) return initial;
  return fetched?.gameId === gameId ? fetched.data : null;
}

/** Whether there's a factors section to show */
export const hasFactors = (game: GameOddsVM, data: PredictionFactorsVM | null) =>
  !!data && data.factors.length > 0 && !!game.homeTeam && !!game.awayTeam;
