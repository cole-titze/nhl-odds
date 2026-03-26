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

export function computeStrategyResult(name: string, bets: BetResult[]): StrategyResult {
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

export type StrategyType =
  | 'value'
  | 'modelWinner'
  | 'underdog'
  | 'confidence'
  | 'spreadAll'
  | 'spreadValue'
  | 'totalAll'
  | 'totalValue';

export type BetTypeCategory = 'moneyline' | 'spread' | 'overUnder';

export interface StrategyConfig {
  type: StrategyType;
  threshold: number;
}

export const DEFAULT_STRATEGY: StrategyConfig = { type: 'value', threshold: 0.1 };

export interface StrategyOption {
  type: StrategyType;
  label: string;
  betType: BetTypeCategory;
  thresholds?: number[];
  thresholdFormat?: 'pct' | 'goals';
}

export const STRATEGY_OPTIONS: StrategyOption[] = [
  {
    type: 'value',
    label: 'Value Bets',
    betType: 'moneyline',
    thresholds: [0.03, 0.05, 0.07, 0.1, 0.15],
    thresholdFormat: 'pct',
  },
  { type: 'modelWinner', label: 'In-House Winner', betType: 'moneyline' },
  { type: 'underdog', label: 'In-House Underdog', betType: 'moneyline' },
  {
    type: 'confidence',
    label: 'Confidence',
    betType: 'moneyline',
    thresholds: [0.52, 0.55, 0.58, 0.6, 0.65],
    thresholdFormat: 'pct',
  },
  { type: 'spreadAll', label: 'Spread Bet', betType: 'spread' },
  {
    type: 'spreadValue',
    label: 'Spread Value',
    betType: 'spread',
    thresholds: [0.25, 0.5, 0.75, 1.0, 1.5],
    thresholdFormat: 'goals',
  },
  { type: 'totalAll', label: 'O/U Bet', betType: 'overUnder' },
  {
    type: 'totalValue',
    label: 'O/U Value',
    betType: 'overUnder',
    thresholds: [0.25, 0.5, 0.75, 1.0, 1.5],
    thresholdFormat: 'goals',
  },
];

/**
 * Check if a game qualifies as a bet for a given bookmaker under the selected strategy.
 */
export function checkStrategy(
  game: GameOddsVM,
  bm: BookmakerOddsVM,
  strategy: StrategyConfig = DEFAULT_STRATEGY,
): BetFlag | null {
  if (!game.homeTeam || !game.awayTeam) return null;

  switch (strategy.type) {
    case 'value': {
      if (bm.homeOdds <= 0 || bm.awayOdds <= 0) return null;
      const homeModel = game.homeTeam.modelOdds;
      const awayModel = game.awayTeam.modelOdds;
      const homeEdge = homeModel - bm.homeOdds;
      const awayEdge = awayModel - bm.awayOdds;
      if (homeEdge >= strategy.threshold && homeEdge >= awayEdge)
        return { side: 'home', edge: homeEdge };
      if (awayEdge >= strategy.threshold) return { side: 'away', edge: awayEdge };
      return null;
    }
    case 'modelWinner': {
      if (bm.homeOdds <= 0 || bm.awayOdds <= 0) return null;
      const homeModel = game.homeTeam.modelOdds;
      const awayModel = game.awayTeam.modelOdds;
      const side: 'home' | 'away' = homeModel >= 0.5 ? 'home' : 'away';
      const edge = side === 'home' ? homeModel - bm.homeOdds : awayModel - bm.awayOdds;
      return { side, edge };
    }
    case 'underdog': {
      if (bm.homeOdds <= 0 || bm.awayOdds <= 0) return null;
      const homeModel = game.homeTeam.modelOdds;
      const awayModel = game.awayTeam.modelOdds;
      const side: 'home' | 'away' = homeModel >= 0.5 ? 'home' : 'away';
      const bookProb = side === 'home' ? bm.homeOdds : bm.awayOdds;
      if (bookProb >= 0.5) return null;
      const edge = (side === 'home' ? homeModel : awayModel) - bookProb;
      return { side, edge };
    }
    case 'confidence': {
      if (bm.homeOdds <= 0 || bm.awayOdds <= 0) return null;
      const homeModel = game.homeTeam.modelOdds;
      const awayModel = game.awayTeam.modelOdds;
      if (homeModel >= strategy.threshold) return { side: 'home', edge: homeModel - bm.homeOdds };
      if (awayModel >= strategy.threshold) return { side: 'away', edge: awayModel - bm.awayOdds };
      return null;
    }
    case 'spreadAll': {
      if (!bm.homePoint || game.predictedSpread == null) return null;
      // predictedSpread is predicted margin (home - away), homePoint is handicap (opposite sign)
      // home covers when predictedSpread + homePoint > 0
      const coverMargin = game.predictedSpread + bm.homePoint;
      const side: 'home' | 'away' = coverMargin > 0 ? 'home' : 'away';
      return { side, edge: Math.abs(coverMargin) };
    }
    case 'spreadValue': {
      if (!bm.homePoint || game.predictedSpread == null) return null;
      const coverMargin = game.predictedSpread + bm.homePoint;
      const edge = Math.abs(coverMargin);
      if (edge < strategy.threshold) return null;
      const side: 'home' | 'away' = coverMargin > 0 ? 'home' : 'away';
      return { side, edge };
    }
    case 'totalAll': {
      if (!bm.overUnderPoint || game.predictedTotal == null) return null;
      const edge = Math.abs(game.predictedTotal - bm.overUnderPoint);
      const side: 'home' | 'away' = game.predictedTotal > bm.overUnderPoint ? 'home' : 'away';
      return { side, edge };
    }
    case 'totalValue': {
      if (!bm.overUnderPoint || game.predictedTotal == null) return null;
      const edge = Math.abs(game.predictedTotal - bm.overUnderPoint);
      if (edge < strategy.threshold) return null;
      const side: 'home' | 'away' = game.predictedTotal > bm.overUnderPoint ? 'home' : 'away';
      return { side, edge };
    }
  }
}

export function americanToDecimalPayout(price: number): number {
  if (price > 0) return price / 100;
  return 100 / Math.abs(price);
}

// --- Spread strategies ---

export function spreadAlwaysBet(games: GameOddsVM[], bookmaker: string): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam || g.predictedSpread == null) continue;
    const bk = g.bookmakerOdds.find((b) => b.bookmakerName === bookmaker);
    if (!bk || !bk.homePoint) continue;
    const actualMargin = g.homeTeam.goals - g.awayTeam.goals;
    // predictedSpread is margin (home-away), homePoint is handicap (opposite sign)
    // home covers when predictedSpread + homePoint > 0
    const coverMargin = g.predictedSpread + bk.homePoint;
    const betHome = coverMargin > 0;
    const price = betHome ? bk.homePrice : bk.awayPrice;
    const covered = betHome ? actualMargin + bk.homePoint > 0 : actualMargin + bk.awayPoint > 0;
    const payout = covered ? americanToDecimalPayout(price) : -1;
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: betHome ? 'home' : 'away',
      modelProb: g.predictedSpread,
      bookmakerProb: bk.homePoint,
      payout: +payout.toFixed(4),
      won: covered,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('Spread Bet', bets);
}

export function spreadValueOnly(
  games: GameOddsVM[],
  bookmaker: string,
  threshold: number,
): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam || g.predictedSpread == null) continue;
    const bk = g.bookmakerOdds.find((b) => b.bookmakerName === bookmaker);
    if (!bk || !bk.homePoint) continue;
    const coverMargin = g.predictedSpread + bk.homePoint;
    const edge = Math.abs(coverMargin);
    if (edge < threshold) continue;
    const actualMargin = g.homeTeam.goals - g.awayTeam.goals;
    const betHome = coverMargin > 0;
    const price = betHome ? bk.homePrice : bk.awayPrice;
    const covered = betHome ? actualMargin + bk.homePoint > 0 : actualMargin + bk.awayPoint > 0;
    const payout = covered ? americanToDecimalPayout(price) : -1;
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: betHome ? 'home' : 'away',
      modelProb: g.predictedSpread,
      bookmakerProb: bk.homePoint,
      payout: +payout.toFixed(4),
      won: covered,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('Spread Value', bets);
}

// --- Over/Under strategies ---

export function totalAlwaysBet(games: GameOddsVM[], bookmaker: string): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam || g.predictedTotal == null) continue;
    const bk = g.bookmakerOdds.find((b) => b.bookmakerName === bookmaker);
    if (!bk || !bk.overUnderPoint) continue;
    const actualTotal = g.homeTeam.goals + g.awayTeam.goals;
    const betOver = g.predictedTotal > bk.overUnderPoint;
    const price = betOver ? bk.overPrice : bk.underPrice;
    const won = betOver ? actualTotal > bk.overUnderPoint : actualTotal < bk.overUnderPoint;
    const payout = won ? americanToDecimalPayout(price) : -1;
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: betOver ? 'home' : 'away', // reuse: home=over, away=under
      modelProb: g.predictedTotal,
      bookmakerProb: bk.overUnderPoint,
      payout: +payout.toFixed(4),
      won,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('O/U Bet', bets);
}

export function totalValueOnly(
  games: GameOddsVM[],
  bookmaker: string,
  threshold: number,
): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam || g.predictedTotal == null) continue;
    const bk = g.bookmakerOdds.find((b) => b.bookmakerName === bookmaker);
    if (!bk || !bk.overUnderPoint) continue;
    const edge = Math.abs(g.predictedTotal - bk.overUnderPoint);
    if (edge < threshold) continue;
    const actualTotal = g.homeTeam.goals + g.awayTeam.goals;
    const betOver = g.predictedTotal > bk.overUnderPoint;
    const price = betOver ? bk.overPrice : bk.underPrice;
    const won = betOver ? actualTotal > bk.overUnderPoint : actualTotal < bk.overUnderPoint;
    const payout = won ? americanToDecimalPayout(price) : -1;
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: betOver ? 'home' : 'away',
      modelProb: g.predictedTotal,
      bookmakerProb: bk.overUnderPoint,
      payout: +payout.toFixed(4),
      won,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('O/U Value', bets);
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
