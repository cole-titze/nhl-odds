import type { GameOddsVM, BookmakerOddsVM } from '../types';
import { Winner } from '../types';

export interface BetResult {
  gameId: number;
  gameDate: string;
  betSide: 'home' | 'away';
  modelProb: number;
  bookmakerProb: number;
  payout: number;
  won: boolean;
  cumulativePL: number;
}

export interface StrategyResult {
  name: string;
  bets: BetResult[];
  totalBets: number;
  wins: number;
  winRate: number;
  totalPL: number;
  roi: number;
}

interface GameWithOdds {
  game: GameOddsVM;
  homeModelProb: number;
  awayModelProb: number;
  homeBookProb: number;
  awayBookProb: number;
}

function prepareGames(games: GameOddsVM[], bookmaker: string): GameWithOdds[] {
  return games
    .filter((g) => g.hasBeenPlayed && g.homeTeam && g.awayTeam)
    .map((g) => {
      const bk = g.bookmakerOdds.find((b) => b.bookmakerName === bookmaker);
      if (!bk || bk.homeOdds <= 0 || bk.awayOdds <= 0) return null;
      return {
        game: g,
        homeModelProb: g.homeTeam!.modelOdds,
        awayModelProb: g.awayTeam!.modelOdds,
        homeBookProb: bk.homeOdds,
        awayBookProb: bk.awayOdds,
      };
    })
    .filter((g): g is GameWithOdds => g !== null);
}

function makeBet(gwo: GameWithOdds, side: 'home' | 'away'): BetResult {
  const isHome = side === 'home';
  const modelProb = isHome ? gwo.homeModelProb : gwo.awayModelProb;
  const bookmakerProb = isHome ? gwo.homeBookProb : gwo.awayBookProb;
  const won = gwo.game.winner === (isHome ? Winner.HOME : Winner.AWAY);
  const payout = won ? 1 / bookmakerProb - 1 : -1;

  return {
    gameId: gwo.game.id,
    gameDate: gwo.game.gameDate,
    betSide: side,
    modelProb,
    bookmakerProb,
    payout: +payout.toFixed(4),
    won,
    cumulativePL: 0,
  };
}

function computeStrategyResult(name: string, bets: BetResult[]): StrategyResult {
  let cumPL = 0;
  for (const bet of bets) {
    cumPL += bet.payout;
    bet.cumulativePL = +cumPL.toFixed(4);
  }
  const wins = bets.filter((b) => b.won).length;
  return {
    name,
    bets,
    totalBets: bets.length,
    wins,
    winRate: bets.length > 0 ? +(wins / bets.length).toFixed(4) : 0,
    totalPL: +cumPL.toFixed(4),
    roi: bets.length > 0 ? +((cumPL / bets.length) * 100).toFixed(2) : 0,
  };
}

export interface BetFlag {
  side: 'home' | 'away';
  edge: number;
}

export type StrategyType = 'value' | 'modelWinner' | 'underdog' | 'confidence';

export interface StrategyConfig {
  type: StrategyType;
  threshold: number;
}

export const DEFAULT_STRATEGY: StrategyConfig = { type: 'value', threshold: 0.1 };

export const STRATEGY_OPTIONS: { type: StrategyType; label: string; thresholds?: number[] }[] = [
  { type: 'value', label: 'Value Bets', thresholds: [0.03, 0.05, 0.07, 0.1, 0.15] },
  { type: 'modelWinner', label: 'In-House Winner' },
  { type: 'underdog', label: 'In-House Underdog' },
  { type: 'confidence', label: 'Confidence', thresholds: [0.52, 0.55, 0.58, 0.6, 0.65] },
];

/**
 * Check if a game qualifies as a bet for a given bookmaker under the selected strategy.
 */
export function checkStrategy(
  game: GameOddsVM,
  bm: BookmakerOddsVM,
  strategy: StrategyConfig = DEFAULT_STRATEGY,
): BetFlag | null {
  if (!game.homeTeam || !game.awayTeam || bm.homeOdds <= 0 || bm.awayOdds <= 0) return null;

  const homeModel = game.homeTeam.modelOdds;
  const awayModel = game.awayTeam.modelOdds;

  switch (strategy.type) {
    case 'value': {
      const homeEdge = homeModel - bm.homeOdds;
      const awayEdge = awayModel - bm.awayOdds;
      if (homeEdge >= strategy.threshold && homeEdge >= awayEdge)
        return { side: 'home', edge: homeEdge };
      if (awayEdge >= strategy.threshold) return { side: 'away', edge: awayEdge };
      return null;
    }
    case 'modelWinner': {
      const side: 'home' | 'away' = homeModel >= 0.5 ? 'home' : 'away';
      const edge = side === 'home' ? homeModel - bm.homeOdds : awayModel - bm.awayOdds;
      return { side, edge };
    }
    case 'underdog': {
      const side: 'home' | 'away' = homeModel >= 0.5 ? 'home' : 'away';
      const bookProb = side === 'home' ? bm.homeOdds : bm.awayOdds;
      if (bookProb >= 0.5) return null;
      const edge = (side === 'home' ? homeModel : awayModel) - bookProb;
      return { side, edge };
    }
    case 'confidence': {
      if (homeModel >= strategy.threshold) return { side: 'home', edge: homeModel - bm.homeOdds };
      if (awayModel >= strategy.threshold) return { side: 'away', edge: awayModel - bm.awayOdds };
      return null;
    }
  }
}

export function alwaysBetModelWinner(games: GameOddsVM[], bookmaker: string): StrategyResult {
  const prepared = prepareGames(games, bookmaker);
  const bets = prepared.map((gwo) => {
    const side = gwo.homeModelProb >= 0.5 ? 'home' : 'away';
    return makeBet(gwo, side);
  });
  return computeStrategyResult('In-House Winner', bets);
}

export function valueBetsOnly(
  games: GameOddsVM[],
  bookmaker: string,
  threshold: number,
): StrategyResult {
  const prepared = prepareGames(games, bookmaker);
  const bets: BetResult[] = [];
  for (const gwo of prepared) {
    const homeEdge = gwo.homeModelProb - gwo.homeBookProb;
    const awayEdge = gwo.awayModelProb - gwo.awayBookProb;
    if (homeEdge >= threshold && homeEdge >= awayEdge) {
      bets.push(makeBet(gwo, 'home'));
    } else if (awayEdge >= threshold) {
      bets.push(makeBet(gwo, 'away'));
    }
  }
  return computeStrategyResult('Value Bets', bets);
}

export function modelUnderdogPicks(games: GameOddsVM[], bookmaker: string): StrategyResult {
  const prepared = prepareGames(games, bookmaker);
  const bets: BetResult[] = [];
  for (const gwo of prepared) {
    const modelSide: 'home' | 'away' = gwo.homeModelProb >= 0.5 ? 'home' : 'away';
    const bookProb = modelSide === 'home' ? gwo.homeBookProb : gwo.awayBookProb;
    if (bookProb < 0.5) {
      bets.push(makeBet(gwo, modelSide));
    }
  }
  return computeStrategyResult('In-House Underdog', bets);
}

export function confidenceFilter(
  games: GameOddsVM[],
  bookmaker: string,
  minConfidence: number,
): StrategyResult {
  const prepared = prepareGames(games, bookmaker);
  const bets: BetResult[] = [];
  for (const gwo of prepared) {
    const homeConf = gwo.homeModelProb;
    const awayConf = gwo.awayModelProb;
    if (homeConf >= minConfidence) {
      bets.push(makeBet(gwo, 'home'));
    } else if (awayConf >= minConfidence) {
      bets.push(makeBet(gwo, 'away'));
    }
  }
  return computeStrategyResult('Confidence', bets);
}
