import type { GameOddsVM, BookmakerOddsVM } from '../types';
import { Winner } from '../types';
import { americanToDecimalPayout, computeStrategyResult } from './bettingStrategies';
import type { BetResult, StrategyResult, StrategyType } from './bettingStrategies';

export const MODEL_REFERENCE = 'In-House Model';

export interface CrossBookEntry {
  gameId: number;
  gameDate: string;
  homeTeam: string;
  awayTeam: string;
  betSide: 'home' | 'away' | 'over' | 'under';
  refValue: number;
  betValue: number;
  edge: number;
  payout: number | null;
  won: boolean | null;
  upcoming: boolean;
}

function removeVig(homeProb: number, awayProb: number): { home: number; away: number } {
  const total = homeProb + awayProb;
  if (total <= 0) return { home: 0, away: 0 };
  return { home: homeProb / total, away: awayProb / total };
}

function getRefOdds(game: GameOddsVM, refBookmaker: string): Partial<BookmakerOddsVM> | undefined {
  if (refBookmaker === MODEL_REFERENCE) {
    if (!game.homeTeam || !game.awayTeam) return undefined;
    return {
      bookmakerName: MODEL_REFERENCE,
      homeOdds: game.homeTeam.modelOdds ?? 0,
      awayOdds: game.awayTeam.modelOdds ?? 0,
      homePoint: game.predictedSpread ?? 0,
      awayPoint: game.predictedSpread != null ? -game.predictedSpread : 0,
      homePrice: 0,
      awayPrice: 0,
      overUnderPoint: game.predictedTotal ?? 0,
      overPrice: 0,
      underPrice: 0,
    };
  }
  return game.bookmakerOdds.find((b) => b.bookmakerName === refBookmaker);
}

function findBookmaker(game: GameOddsVM, name: string): BookmakerOddsVM | undefined {
  return game.bookmakerOdds.find((b) => b.bookmakerName === name);
}

// Whether the reference is the model (already vig-free) or a bookmaker (needs vig removal)
function getRefProbs(
  ref: Partial<BookmakerOddsVM>,
  refBookmaker: string,
): { home: number; away: number } {
  if (refBookmaker === MODEL_REFERENCE) {
    return { home: ref.homeOdds ?? 0, away: ref.awayOdds ?? 0 };
  }
  return removeVig(ref.homeOdds ?? 0, ref.awayOdds ?? 0);
}

// --- Moneyline strategies ---

export function crossBookMoneyline(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
  edgeThreshold: number,
): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam) continue;
    const ref = getRefOdds(g, refBookmaker);
    const bet = findBookmaker(g, betBookmaker);
    if (!ref || !bet) continue;
    if (
      (ref.homeOdds ?? 0) <= 0 ||
      (ref.awayOdds ?? 0) <= 0 ||
      bet.homeOdds <= 0 ||
      bet.awayOdds <= 0
    )
      continue;

    const trueRef = getRefProbs(ref, refBookmaker);
    const homeEdge = trueRef.home - bet.homeOdds;
    const awayEdge = trueRef.away - bet.awayOdds;

    let side: 'home' | 'away' | null = null;
    if (homeEdge >= edgeThreshold && homeEdge >= awayEdge) side = 'home';
    else if (awayEdge >= edgeThreshold) side = 'away';
    if (!side) continue;

    const isHome = side === 'home';
    const betProb = isHome ? bet.homeOdds : bet.awayOdds;
    const won = g.winner === (isHome ? Winner.HOME : Winner.AWAY);
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: side,
      modelProb: isHome ? trueRef.home : trueRef.away,
      bookmakerProb: betProb,
      payout: +(won ? 1 / betProb - 1 : -1).toFixed(4),
      won,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('Value Bets', bets);
}

export function crossBookWinner(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam) continue;
    const ref = getRefOdds(g, refBookmaker);
    const bet = findBookmaker(g, betBookmaker);
    if (!ref || !bet) continue;
    if (
      (ref.homeOdds ?? 0) <= 0 ||
      (ref.awayOdds ?? 0) <= 0 ||
      bet.homeOdds <= 0 ||
      bet.awayOdds <= 0
    )
      continue;

    const trueRef = getRefProbs(ref, refBookmaker);
    const side: 'home' | 'away' = trueRef.home >= 0.5 ? 'home' : 'away';
    const isHome = side === 'home';
    const betProb = isHome ? bet.homeOdds : bet.awayOdds;
    const won = g.winner === (isHome ? Winner.HOME : Winner.AWAY);
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: side,
      modelProb: isHome ? trueRef.home : trueRef.away,
      bookmakerProb: betProb,
      payout: +(won ? 1 / betProb - 1 : -1).toFixed(4),
      won,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('In-House Winner', bets);
}

export function crossBookUnderdog(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam) continue;
    const ref = getRefOdds(g, refBookmaker);
    const bet = findBookmaker(g, betBookmaker);
    if (!ref || !bet) continue;
    if (
      (ref.homeOdds ?? 0) <= 0 ||
      (ref.awayOdds ?? 0) <= 0 ||
      bet.homeOdds <= 0 ||
      bet.awayOdds <= 0
    )
      continue;

    const trueRef = getRefProbs(ref, refBookmaker);
    const side: 'home' | 'away' = trueRef.home >= 0.5 ? 'home' : 'away';
    const isHome = side === 'home';
    const betProb = isHome ? bet.homeOdds : bet.awayOdds;
    // Only bet when the ref's pick is the bookmaker's underdog
    if (betProb >= 0.5) continue;
    const won = g.winner === (isHome ? Winner.HOME : Winner.AWAY);
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: side,
      modelProb: isHome ? trueRef.home : trueRef.away,
      bookmakerProb: betProb,
      payout: +(won ? 1 / betProb - 1 : -1).toFixed(4),
      won,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('In-House Underdog', bets);
}

export function crossBookConfidence(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
  minConfidence: number,
): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam) continue;
    const ref = getRefOdds(g, refBookmaker);
    const bet = findBookmaker(g, betBookmaker);
    if (!ref || !bet) continue;
    if (
      (ref.homeOdds ?? 0) <= 0 ||
      (ref.awayOdds ?? 0) <= 0 ||
      bet.homeOdds <= 0 ||
      bet.awayOdds <= 0
    )
      continue;

    const trueRef = getRefProbs(ref, refBookmaker);
    let side: 'home' | 'away' | null = null;
    if (trueRef.home >= minConfidence) side = 'home';
    else if (trueRef.away >= minConfidence) side = 'away';
    if (!side) continue;

    const isHome = side === 'home';
    const betProb = isHome ? bet.homeOdds : bet.awayOdds;
    const won = g.winner === (isHome ? Winner.HOME : Winner.AWAY);
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: side,
      modelProb: isHome ? trueRef.home : trueRef.away,
      bookmakerProb: betProb,
      payout: +(won ? 1 / betProb - 1 : -1).toFixed(4),
      won,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('Confidence', bets);
}

// --- Spread strategies ---

export function crossBookSpread(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
  edgeThreshold: number,
): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam) continue;
    const ref = getRefOdds(g, refBookmaker);
    const bet = findBookmaker(g, betBookmaker);
    if (!ref || !bet) continue;
    if (!ref.homePoint || !bet.homePoint) continue;

    const lineDiff = ref.homePoint - bet.homePoint;
    if (Math.abs(lineDiff) < edgeThreshold) continue;

    const betHome = lineDiff > 0;
    const price = betHome ? bet.homePrice : bet.awayPrice;
    const actualMargin = g.homeTeam.goals - g.awayTeam.goals;
    const covered = betHome ? actualMargin + bet.homePoint > 0 : actualMargin + bet.awayPoint > 0;
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: betHome ? 'home' : 'away',
      modelProb: ref.homePoint,
      bookmakerProb: bet.homePoint,
      payout: +(covered ? americanToDecimalPayout(price) : -1).toFixed(4),
      won: covered,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('Spread Value', bets);
}

export function crossBookSpreadAll(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
): StrategyResult {
  return crossBookSpread(games, refBookmaker, betBookmaker, 0);
}

// --- Over/Under strategies ---

export function crossBookTotal(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
  edgeThreshold: number,
): StrategyResult {
  const bets: BetResult[] = [];
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam) continue;
    const ref = getRefOdds(g, refBookmaker);
    const bet = findBookmaker(g, betBookmaker);
    if (!ref || !bet) continue;
    if (!ref.overUnderPoint || !bet.overUnderPoint) continue;

    const lineDiff = ref.overUnderPoint - bet.overUnderPoint;
    if (Math.abs(lineDiff) < edgeThreshold) continue;

    const betOver = lineDiff > 0;
    const price = betOver ? bet.overPrice : bet.underPrice;
    const actualTotal = g.homeTeam.goals + g.awayTeam.goals;
    const won = betOver ? actualTotal > bet.overUnderPoint : actualTotal < bet.overUnderPoint;
    bets.push({
      gameId: g.id,
      gameDate: g.gameDate,
      betSide: betOver ? 'home' : 'away',
      modelProb: ref.overUnderPoint,
      bookmakerProb: bet.overUnderPoint,
      payout: +(won ? americanToDecimalPayout(price) : -1).toFixed(4),
      won,
      cumulativePL: 0,
    });
  }
  return computeStrategyResult('O/U Value', bets);
}

export function crossBookTotalAll(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
): StrategyResult {
  return crossBookTotal(games, refBookmaker, betBookmaker, 0);
}

// --- Unified runner ---

export function runStrategy(
  games: GameOddsVM[],
  strategyType: StrategyType,
  refBookmaker: string,
  betBookmaker: string,
  threshold: number,
): StrategyResult {
  switch (strategyType) {
    case 'value':
      return crossBookMoneyline(games, refBookmaker, betBookmaker, threshold);
    case 'modelWinner':
      return crossBookWinner(games, refBookmaker, betBookmaker);
    case 'underdog':
      return crossBookUnderdog(games, refBookmaker, betBookmaker);
    case 'confidence':
      return crossBookConfidence(games, refBookmaker, betBookmaker, threshold);
    case 'spreadAll':
      return crossBookSpreadAll(games, refBookmaker, betBookmaker);
    case 'spreadValue':
      return crossBookSpread(games, refBookmaker, betBookmaker, threshold);
    case 'totalAll':
      return crossBookTotalAll(games, refBookmaker, betBookmaker);
    case 'totalValue':
      return crossBookTotal(games, refBookmaker, betBookmaker, threshold);
  }
}

// --- Bet log (played + upcoming) ---

export function crossBookLog(
  games: GameOddsVM[],
  strategyType: StrategyType,
  refBookmaker: string,
  betBookmaker: string,
  threshold: number,
): CrossBookEntry[] {
  const betType =
    strategyType === 'spreadAll' || strategyType === 'spreadValue'
      ? 'spread'
      : strategyType === 'totalAll' || strategyType === 'totalValue'
        ? 'overUnder'
        : 'moneyline';

  const entries: CrossBookEntry[] = [];

  for (const g of games) {
    if (!g.homeTeam || !g.awayTeam) continue;
    const ref = getRefOdds(g, refBookmaker);
    const bet = findBookmaker(g, betBookmaker);
    if (!ref || !bet) continue;

    const homeName = `${g.homeTeam.locationName} ${g.homeTeam.teamName}`;
    const awayName = `${g.awayTeam.locationName} ${g.awayTeam.teamName}`;

    if (betType === 'moneyline') {
      if (
        (ref.homeOdds ?? 0) <= 0 ||
        (ref.awayOdds ?? 0) <= 0 ||
        bet.homeOdds <= 0 ||
        bet.awayOdds <= 0
      )
        continue;
      const trueRef = getRefProbs(ref, refBookmaker);

      let side: 'home' | 'away' | null = null;
      let edge = 0;
      let refValue = 0;
      let betValue = 0;

      if (strategyType === 'value') {
        const homeEdge = trueRef.home - bet.homeOdds;
        const awayEdge = trueRef.away - bet.awayOdds;
        if (homeEdge >= threshold && homeEdge >= awayEdge) {
          side = 'home';
          edge = homeEdge;
          refValue = trueRef.home;
          betValue = bet.homeOdds;
        } else if (awayEdge >= threshold) {
          side = 'away';
          edge = awayEdge;
          refValue = trueRef.away;
          betValue = bet.awayOdds;
        }
      } else if (strategyType === 'modelWinner') {
        side = trueRef.home >= 0.5 ? 'home' : 'away';
        refValue = side === 'home' ? trueRef.home : trueRef.away;
        betValue = side === 'home' ? bet.homeOdds : bet.awayOdds;
        edge = refValue - betValue;
      } else if (strategyType === 'underdog') {
        side = trueRef.home >= 0.5 ? 'home' : 'away';
        betValue = side === 'home' ? bet.homeOdds : bet.awayOdds;
        if (betValue >= 0.5) continue;
        refValue = side === 'home' ? trueRef.home : trueRef.away;
        edge = refValue - betValue;
      } else if (strategyType === 'confidence') {
        if (trueRef.home >= threshold) {
          side = 'home';
          refValue = trueRef.home;
          betValue = bet.homeOdds;
        } else if (trueRef.away >= threshold) {
          side = 'away';
          refValue = trueRef.away;
          betValue = bet.awayOdds;
        }
        if (side) edge = refValue - betValue;
      }

      if (!side) continue;

      if (g.hasBeenPlayed) {
        const won = g.winner === (side === 'home' ? Winner.HOME : Winner.AWAY);
        const payout = won ? 1 / betValue - 1 : -1;
        entries.push({
          gameId: g.id,
          gameDate: g.gameDate,
          homeTeam: homeName,
          awayTeam: awayName,
          betSide: side,
          refValue,
          betValue,
          edge,
          payout: +payout.toFixed(4),
          won,
          upcoming: false,
        });
      } else {
        entries.push({
          gameId: g.id,
          gameDate: g.gameDate,
          homeTeam: homeName,
          awayTeam: awayName,
          betSide: side,
          refValue,
          betValue,
          edge,
          payout: null,
          won: null,
          upcoming: true,
        });
      }
    } else if (betType === 'spread') {
      if (!ref.homePoint || !bet.homePoint) continue;
      const lineDiff = ref.homePoint - bet.homePoint;
      const effectiveThreshold = strategyType === 'spreadAll' ? 0 : threshold;
      if (Math.abs(lineDiff) < effectiveThreshold) continue;

      const betHome = lineDiff > 0;
      const side: 'home' | 'away' = betHome ? 'home' : 'away';

      if (g.hasBeenPlayed) {
        const price = betHome ? bet.homePrice : bet.awayPrice;
        const actualMargin = g.homeTeam.goals - g.awayTeam.goals;
        const covered = betHome
          ? actualMargin + bet.homePoint > 0
          : actualMargin + bet.awayPoint > 0;
        const payout = covered ? americanToDecimalPayout(price) : -1;
        entries.push({
          gameId: g.id,
          gameDate: g.gameDate,
          homeTeam: homeName,
          awayTeam: awayName,
          betSide: side,
          refValue: ref.homePoint,
          betValue: bet.homePoint,
          edge: Math.abs(lineDiff),
          payout: +payout.toFixed(4),
          won: covered,
          upcoming: false,
        });
      } else {
        entries.push({
          gameId: g.id,
          gameDate: g.gameDate,
          homeTeam: homeName,
          awayTeam: awayName,
          betSide: side,
          refValue: ref.homePoint,
          betValue: bet.homePoint,
          edge: Math.abs(lineDiff),
          payout: null,
          won: null,
          upcoming: true,
        });
      }
    } else {
      if (!ref.overUnderPoint || !bet.overUnderPoint) continue;
      const lineDiff = ref.overUnderPoint - bet.overUnderPoint;
      const effectiveThreshold = strategyType === 'totalAll' ? 0 : threshold;
      if (Math.abs(lineDiff) < effectiveThreshold) continue;

      const betOver = lineDiff > 0;
      const side: 'over' | 'under' = betOver ? 'over' : 'under';

      if (g.hasBeenPlayed) {
        const price = betOver ? bet.overPrice : bet.underPrice;
        const actualTotal = g.homeTeam.goals + g.awayTeam.goals;
        const won = betOver ? actualTotal > bet.overUnderPoint : actualTotal < bet.overUnderPoint;
        const payout = won ? americanToDecimalPayout(price) : -1;
        entries.push({
          gameId: g.id,
          gameDate: g.gameDate,
          homeTeam: homeName,
          awayTeam: awayName,
          betSide: side,
          refValue: ref.overUnderPoint,
          betValue: bet.overUnderPoint,
          edge: Math.abs(lineDiff),
          payout: +payout.toFixed(4),
          won,
          upcoming: false,
        });
      } else {
        entries.push({
          gameId: g.id,
          gameDate: g.gameDate,
          homeTeam: homeName,
          awayTeam: awayName,
          betSide: side,
          refValue: ref.overUnderPoint,
          betValue: bet.overUnderPoint,
          edge: Math.abs(lineDiff),
          payout: null,
          won: null,
          upcoming: true,
        });
      }
    }
  }

  return entries;
}

// --- Coverage ---

export function getCoverage(
  games: GameOddsVM[],
  refBookmaker: string,
  betBookmaker: string,
): { matched: number; total: number } {
  let matched = 0;
  let total = 0;
  for (const g of games) {
    if (!g.hasBeenPlayed || !g.homeTeam || !g.awayTeam) continue;
    total++;
    const ref = getRefOdds(g, refBookmaker);
    const bet = findBookmaker(g, betBookmaker);
    if (ref && bet && (ref.homeOdds ?? 0) > 0 && bet.homeOdds > 0) matched++;
  }
  return { matched, total };
}
