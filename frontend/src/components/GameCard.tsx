import type { PointerEvent } from 'react';
import { Link } from 'react-router-dom';
import type { GameOddsVM, BookmakerOddsVM } from '../types';
import { Winner } from '../types';
import { wasCorrectlyPredicted } from '../utils/predictions';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { getModelName } from '../utils/modelNames';
import { formatOdds, formatAmericanOdds } from '../utils/oddsFormat';
import type { OddsType } from '../pages/GamesPage';
import {
  getBestBetFlag,
  PINNED_BOOKMAKERS,
  STRATEGY_OPTIONS,
  type BestBetFlag,
  type StrategyConfig,
} from '../utils/bettingStrategies';
import { useStrategy } from '../contexts/StrategyContext';
import { TeamLogo } from './TeamLogo';

type BetOutcome = 'won' | 'lost' | 'push';

interface GameCardProps {
  game: GameOddsVM;
  oddsType?: OddsType;
}

function TeamSide({
  team,
  isWinner,
  played,
}: {
  team: GameOddsVM['homeTeam'];
  isWinner: boolean;
  played: boolean;
}) {
  if (!team)
    return <div className="flex-1 text-center text-surface-500 dark:text-surface-400">TBD</div>;

  return (
    <div
      className={`flex-1 flex flex-col items-center gap-2 ${isWinner && played ? 'font-bold' : ''}`}
    >
      <div className="relative">
        <TeamLogo src={team.logoUri} alt="" className="h-20 w-20 object-contain drop-shadow-lg" />
        {isWinner && played && (
          <div
            aria-hidden="true"
            className="absolute -bottom-1 -right-1 w-4 h-4 rounded-full bg-accent-500 flex items-center justify-center"
          >
            <svg
              className="w-2.5 h-2.5 text-white"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={3}
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
            </svg>
          </div>
        )}
      </div>
      <div className="text-xs font-medium text-center leading-tight text-surface-600 dark:text-surface-400">
        {team.locationName}
        <br />
        <span className="text-surface-900 dark:text-white text-sm">{team.teamName}</span>
        {isWinner && played && <span className="sr-only"> (winner)</span>}
      </div>
    </div>
  );
}

function formatPoint(point: number): string {
  return point > 0 ? `+${point}` : `${point}`;
}

function BookmakerOddsRow({
  bm,
  oddsType,
  format,
  highlighted,
}: {
  bm: BookmakerOddsVM;
  oddsType: OddsType;
  format: Parameters<typeof formatOdds>[1];
  highlighted: boolean;
}) {
  const cellClass = 'text-center stat-number text-surface-500 dark:text-surface-400';
  const nameClass = `text-center truncate px-1 ${
    highlighted
      ? 'font-bold text-surface-700 dark:text-surface-200'
      : 'text-surface-500 dark:text-surface-400'
  }`;

  if (oddsType === 'spread') {
    return (
      <div className="grid grid-cols-3 items-center text-[11px] font-mono mt-1">
        <span className={cellClass}>
          {formatPoint(bm.awayPoint)} ({formatAmericanOdds(bm.awayPrice, format)})
        </span>
        <span className={nameClass}>{bm.bookmakerName}</span>
        <span className={cellClass}>
          {formatPoint(bm.homePoint)} ({formatAmericanOdds(bm.homePrice, format)})
        </span>
      </div>
    );
  }

  if (oddsType === 'overUnder') {
    return (
      <div className="grid grid-cols-3 items-center text-[11px] font-mono mt-1">
        <span className={cellClass}>O {formatAmericanOdds(bm.overPrice, format)}</span>
        <span className={nameClass}>
          {bm.bookmakerName} ({bm.overUnderPoint})
        </span>
        <span className={cellClass}>U {formatAmericanOdds(bm.underPrice, format)}</span>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-3 items-center text-[11px] font-mono mt-1">
      <span className={cellClass}>{formatOdds(bm.awayOdds, format)}</span>
      <span className={nameClass}>{bm.bookmakerName}</span>
      <span className={cellClass}>{formatOdds(bm.homeOdds, format)}</span>
    </div>
  );
}

// Grades the bet against the line of the bookmaker that produced it.
function strategyBetOutcome(
  game: GameOddsVM,
  bet: BestBetFlag,
  strategy: StrategyConfig,
): BetOutcome {
  const opt = STRATEGY_OPTIONS.find((o) => o.type === strategy.type);
  if (!opt || !game.homeTeam || !game.awayTeam) return 'lost';
  const bk = game.bookmakerOdds?.find((b) => b.bookmakerName === bet.bookmaker);

  if (opt.betType === 'spread') {
    if (!bk) return 'lost';
    const homeMargin = game.homeTeam.goals - game.awayTeam.goals;
    const covered = bet.side === 'home' ? homeMargin + bk.homePoint : -homeMargin + bk.awayPoint;
    return covered > 0 ? 'won' : covered < 0 ? 'lost' : 'push';
  }

  if (opt.betType === 'overUnder') {
    if (!bk) return 'lost';
    const total = game.homeTeam.goals + game.awayTeam.goals;
    if (total === bk.overUnderPoint) return 'push';
    return (bet.side === 'home') === total > bk.overUnderPoint ? 'won' : 'lost';
  }

  return game.winner === (bet.side === 'home' ? Winner.HOME : Winner.AWAY) ? 'won' : 'lost';
}

const BADGE_CLASSES: Record<BetOutcome | 'upcoming', string> = {
  upcoming: 'bg-blue-500/10 text-blue-700 dark:text-blue-400',
  won: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-400',
  lost: 'bg-red-500/10 text-red-700 dark:text-red-400',
  push: 'bg-surface-500/10 text-surface-600 dark:text-surface-400',
};

const BADGE_SR_LABELS: Record<BetOutcome | 'upcoming', string> = {
  upcoming: 'Suggested bet:',
  won: 'Bet won:',
  lost: 'Bet lost:',
  push: 'Bet pushed:',
};

const CHECK_CIRCLE_PATH =
  'M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z';

const BADGE_ICONS: Record<BetOutcome | 'upcoming', string> = {
  upcoming: CHECK_CIRCLE_PATH,
  won: CHECK_CIRCLE_PATH,
  // x-circle
  lost: 'M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z',
  // minus-circle
  push: 'M10 18a8 8 0 100-16 8 8 0 000 16zM7 9a1 1 0 000 2h6a1 1 0 100-2H7z',
};

// Marks whether the model's moneyline pick came true, so the green/red odds
// color isn't the only cue.
function PickMark({ correct }: { correct: boolean | null }) {
  if (correct == null) return null;
  return (
    <>
      <svg
        aria-hidden="true"
        viewBox="0 0 20 20"
        fill="currentColor"
        className={`inline-block w-3 h-3 mr-0.5 -mt-px align-middle ${
          correct ? 'text-emerald-700 dark:text-emerald-400' : 'text-red-700 dark:text-red-400'
        }`}
      >
        <path
          fillRule="evenodd"
          d={correct ? BADGE_ICONS.won : BADGE_ICONS.lost}
          clipRule="evenodd"
        />
      </svg>
      <span className="sr-only">{correct ? 'Correct pick: ' : 'Missed pick: '}</span>
    </>
  );
}

function setRippleOrigin(e: PointerEvent<HTMLElement>) {
  const rect = e.currentTarget.getBoundingClientRect();
  e.currentTarget.style.setProperty(
    '--ripple-x',
    `${((e.clientX - rect.left) / rect.width) * 100}%`,
  );
  e.currentTarget.style.setProperty(
    '--ripple-y',
    `${((e.clientY - rect.top) / rect.height) * 100}%`,
  );
}

export function GameCard({ game, oddsType = 'moneyline' }: GameCardProps) {
  const { format } = useOddsFormatContext();
  const { strategy } = useStrategy();
  const valueBet = getBestBetFlag(game, strategy);

  const badgeState: BetOutcome | 'upcoming' | null = !valueBet
    ? null
    : game.hasBeenPlayed
      ? strategyBetOutcome(game, valueBet, strategy)
      : 'upcoming';

  const pickCorrect = game.hasBeenPlayed ? wasCorrectlyPredicted(game) : null;
  const oddsColor = !game.hasBeenPlayed
    ? 'text-accent-600 dark:text-accent-400'
    : wasCorrectlyPredicted(game)
      ? 'text-emerald-700 dark:text-emerald-400'
      : 'text-red-700 dark:text-red-400';

  return (
    <Link
      to={`/game/${game.id}`}
      state={{ game }}
      className={`card-hover glass rounded-xl px-5 py-4 block transition-all duration-200 active:scale-[0.98] border-surface-200 dark:border-white/[0.06]`}
      // Pointer (not mouse) events, so a touch sets the origin where the finger lands
      onPointerEnter={setRippleOrigin}
      onPointerDown={setRippleOrigin}
    >
      {game.gameType === 3 && (
        <div className="flex justify-center mb-2">
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-amber-500/10 text-amber-800 dark:text-amber-400 text-[10px] font-bold uppercase tracking-widest">
            <svg viewBox="0 0 24 24" className="w-3 h-3" fill="currentColor" aria-hidden="true">
              {/* Bowl — smaller, narrower */}
              <path d="M8 2h8v3c0 1.5-1.5 2.5-4 2.5S8 6.5 8 5V2z" />
              {/* Neck */}
              <rect x="10.5" y="7.5" width="3" height="1.5" />
              {/* Bands */}
              <rect x="7" y="9" width="10" height="1.6" rx="0.4" />
              <rect x="7" y="11" width="10" height="1.6" rx="0.4" />
              <rect x="7" y="13" width="10" height="1.6" rx="0.4" />
              <rect x="7" y="15" width="10" height="1.6" rx="0.4" />
              {/* Base */}
              <rect x="5.5" y="17" width="13" height="1.5" rx="0.5" />
              <rect x="4" y="19" width="16" height="1.5" rx="0.5" />
            </svg>
            Playoffs
          </span>
        </div>
      )}
      {valueBet && badgeState && (
        <div className="flex justify-center mb-3">
          <span
            className={`inline-flex flex-wrap justify-center items-center gap-x-1.5 gap-y-0.5 max-w-full px-2.5 py-1 rounded-full text-[11px] font-bold uppercase tracking-wide ${BADGE_CLASSES[badgeState]}`}
          >
            <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
              <path fillRule="evenodd" d={BADGE_ICONS[badgeState]} clipRule="evenodd" />
            </svg>
            <span className="sr-only">{BADGE_SR_LABELS[badgeState]}</span>
            {(() => {
              const opt = STRATEGY_OPTIONS.find((o) => o.type === strategy.type);
              if (opt?.betType === 'overUnder') {
                return `${valueBet.side === 'home' ? 'Over' : 'Under'} +${valueBet.edge.toFixed(1)}`;
              }
              if (opt?.betType === 'spread') {
                const label =
                  valueBet.side === 'home' ? game.homeTeam?.teamName : game.awayTeam?.teamName;
                return `${label} +${valueBet.edge.toFixed(1)}`;
              }
              const label =
                valueBet.side === 'home' ? game.homeTeam?.teamName : game.awayTeam?.teamName;
              return `Bet ${label} +${(valueBet.edge * 100).toFixed(0)}%`;
            })()}
            <span className="pl-1.5 border-l border-current/30 font-semibold">
              {valueBet.bookmakers.join(' · ')}
            </span>
            {badgeState === 'push' && <span>· Push</span>}
          </span>
        </div>
      )}
      <div className="grid grid-cols-3 items-center">
        <TeamSide
          team={game.awayTeam}
          isWinner={game.hasBeenPlayed && game.winner === Winner.AWAY}
          played={game.hasBeenPlayed}
        />
        <div className="flex flex-col items-center gap-1 px-2">
          <div
            className={`text-[10px] font-mono font-bold tracking-widest uppercase ${
              game.hasBeenPlayed
                ? 'text-surface-500 dark:text-surface-400'
                : 'text-accent-600 dark:text-accent-400'
            }`}
          >
            {game.hasBeenPlayed ? 'Final' : 'VS'}
          </div>
          {game.hasBeenPlayed ? (
            <div className="stat-number text-lg text-surface-900 dark:text-white">
              {game.awayTeam?.goals ?? 0}
              <span className="text-surface-500 dark:text-surface-400 mx-1">-</span>
              {game.homeTeam?.goals ?? 0}
            </div>
          ) : (
            game.gameDate && (
              <div className="text-[11px] font-mono text-surface-500 dark:text-surface-400">
                {new Date(game.gameDate).toLocaleTimeString([], {
                  hour: 'numeric',
                  minute: '2-digit',
                })}
              </div>
            )
          )}
        </div>
        <TeamSide
          team={game.homeTeam}
          isWinner={game.hasBeenPlayed && game.winner === Winner.HOME}
          played={game.hasBeenPlayed}
        />
        <div
          className={`col-span-3 mt-2 ${game.gameType !== 3 ? 'pt-2 border-t border-surface-200/50 dark:border-white/[0.04]' : ''}`}
        >
          {game.gameType !== 3 && (
            <div className="grid grid-cols-3 items-center text-[11px] font-mono">
              {oddsType === 'moneyline' ? (
                <>
                  <span className={`text-center stat-number ${oddsColor}`}>
                    {game.awayTeam ? formatOdds(game.awayTeam.modelOdds, format) : '-'}
                  </span>
                  <span className="text-center text-surface-500 dark:text-surface-400">
                    <PickMark correct={pickCorrect} />
                    {getModelName(game.modelId)}
                  </span>
                  <span className={`text-center stat-number ${oddsColor}`}>
                    {game.homeTeam ? formatOdds(game.homeTeam.modelOdds, format) : '-'}
                  </span>
                </>
              ) : oddsType === 'spread' ? (
                <>
                  <span className={`text-center stat-number ${oddsColor}`}>
                    {game.predictedSpread != null
                      ? formatPoint(+game.predictedSpread.toFixed(2))
                      : '-'}
                  </span>
                  <span className="text-center text-surface-500 dark:text-surface-400">
                    <PickMark correct={pickCorrect} />
                    {getModelName(game.modelId)}
                  </span>
                  <span className={`text-center stat-number ${oddsColor}`}>
                    {game.predictedSpread != null
                      ? formatPoint(+(-game.predictedSpread).toFixed(2))
                      : '-'}
                  </span>
                </>
              ) : (
                <>
                  <span className={`text-center stat-number ${oddsColor}`}>{'-'}</span>
                  <span className="text-center text-surface-500 dark:text-surface-400">
                    <PickMark correct={pickCorrect} />
                    {getModelName(game.modelId)}
                    {game.predictedTotal != null ? ` (${game.predictedTotal.toFixed(1)})` : ''}
                  </span>
                  <span className={`text-center stat-number ${oddsColor}`}>{'-'}</span>
                </>
              )}
            </div>
          )}
          {game.bookmakerOdds?.length > 0 &&
            (() => {
              const shown = PINNED_BOOKMAKERS.map((name) =>
                game.bookmakerOdds.find((bm) => bm.bookmakerName === name),
              ).filter(Boolean) as BookmakerOddsVM[];
              return (
                <>
                  {shown.map((bm) => (
                    <BookmakerOddsRow
                      key={bm.bookmakerName}
                      bm={bm}
                      oddsType={oddsType}
                      format={format}
                      highlighted={valueBet?.bookmakers.includes(bm.bookmakerName) ?? false}
                    />
                  ))}
                </>
              );
            })()}
        </div>
      </div>
    </Link>
  );
}
