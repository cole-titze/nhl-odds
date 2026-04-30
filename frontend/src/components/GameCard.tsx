import { Link } from 'react-router-dom';
import type { GameOddsVM, BookmakerOddsVM } from '../types';
import { Winner } from '../types';
import { wasCorrectlyPredicted } from '../utils/predictions';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { getModelName } from '../utils/modelNames';
import { formatOdds, formatAmericanOdds } from '../utils/oddsFormat';
import type { OddsType } from '../pages/GamesPage';
import {
  checkStrategy,
  STRATEGY_OPTIONS,
  type BetFlag,
  type StrategyConfig,
} from '../utils/bettingStrategies';
import { useStrategy } from '../contexts/StrategyContext';

function getBestBetFlag(
  game: GameOddsVM,
  strategy: StrategyConfig,
): (BetFlag & { bookmaker: string }) | null {
  let best: (BetFlag & { bookmaker: string }) | null = null;
  for (const bm of game.bookmakerOdds ?? []) {
    const flag = checkStrategy(game, bm, strategy);
    if (flag && (!best || flag.edge > best.edge)) {
      best = { ...flag, bookmaker: bm.bookmakerName };
    }
  }
  return best;
}

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
  if (!team) return <div className="flex-1 text-center text-surface-400">TBD</div>;

  return (
    <div
      className={`flex-1 flex flex-col items-center gap-2 ${isWinner && played ? 'font-bold' : ''}`}
    >
      <div className="relative">
        <img
          src={team.logoUri}
          alt={team.teamName}
          className="h-14 w-14 object-contain drop-shadow-lg"
          onError={(e) => {
            (e.target as HTMLImageElement).style.display = 'none';
          }}
        />
        {isWinner && played && (
          <div className="absolute -bottom-1 -right-1 w-4 h-4 rounded-full bg-accent-500 flex items-center justify-center">
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
}: {
  bm: BookmakerOddsVM;
  oddsType: OddsType;
  format: Parameters<typeof formatOdds>[1];
}) {
  const cellClass = 'text-center stat-number text-surface-500 dark:text-surface-400';

  if (oddsType === 'spread') {
    return (
      <div className="grid grid-cols-3 items-center text-[11px] font-mono mt-1">
        <span className={cellClass}>
          {formatPoint(bm.awayPoint)} ({formatAmericanOdds(bm.awayPrice, format)})
        </span>
        <span className="text-center text-surface-400 dark:text-surface-500 truncate px-1">
          {bm.bookmakerName}
        </span>
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
        <span className="text-center text-surface-400 dark:text-surface-500 truncate px-1">
          {bm.bookmakerName} ({bm.overUnderPoint})
        </span>
        <span className={cellClass}>U {formatAmericanOdds(bm.underPrice, format)}</span>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-3 items-center text-[11px] font-mono mt-1">
      <span className={cellClass}>{formatOdds(bm.awayOdds, format)}</span>
      <span className="text-center text-surface-400 dark:text-surface-500 truncate px-1">
        {bm.bookmakerName}
      </span>
      <span className={cellClass}>{formatOdds(bm.homeOdds, format)}</span>
    </div>
  );
}

function strategyBetWon(game: GameOddsVM, bet: BetFlag, strategy: StrategyConfig): boolean {
  const opt = STRATEGY_OPTIONS.find((o) => o.type === strategy.type);
  if (!opt || !game.homeTeam || !game.awayTeam) return false;

  if (opt.betType === 'spread') {
    const bk = game.bookmakerOdds?.find((b) => b.homePoint);
    if (!bk) return false;
    const margin = game.homeTeam.goals - game.awayTeam.goals;
    return bet.side === 'home' ? margin + bk.homePoint > 0 : margin + bk.awayPoint > 0;
  }

  if (opt.betType === 'overUnder') {
    const bk = game.bookmakerOdds?.find((b) => b.overUnderPoint);
    if (!bk) return false;
    const total = game.homeTeam.goals + game.awayTeam.goals;
    return bet.side === 'home' ? total > bk.overUnderPoint : total < bk.overUnderPoint;
  }

  return game.winner === (bet.side === 'home' ? Winner.HOME : Winner.AWAY);
}

export function GameCard({ game, oddsType = 'moneyline' }: GameCardProps) {
  const { format } = useOddsFormatContext();
  const { strategy } = useStrategy();
  const valueBet = getBestBetFlag(game, strategy);

  let cardClass: string;
  if (valueBet) {
    if (!game.hasBeenPlayed) {
      cardClass =
        'border-emerald-500/40 dark:border-emerald-500/20 !bg-emerald-50/30 dark:!bg-emerald-500/[0.03]';
    } else if (strategyBetWon(game, valueBet, strategy)) {
      cardClass =
        'border-emerald-500/40 dark:border-emerald-500/20 !bg-emerald-50/30 dark:!bg-emerald-500/[0.03]';
    } else {
      cardClass = 'border-red-500/40 dark:border-red-500/20 !bg-red-50/30 dark:!bg-red-500/[0.03]';
    }
  } else {
    cardClass = 'border-surface-200 dark:border-white/[0.06]';
  }

  const oddsColor = !game.hasBeenPlayed
    ? 'text-accent-500'
    : wasCorrectlyPredicted(game)
      ? 'text-emerald-500'
      : 'text-red-500';

  return (
    <Link
      to={`/game/${game.id}`}
      state={{ game }}
      className={`glass rounded-xl px-5 py-4 block hover:ring-1 hover:ring-accent-500/30 transition-all ${cardClass}`}
    >
      {game.gameType === 3 && (
        <div className="flex justify-center mb-2">
          <span className="inline-flex items-center px-2 py-0.5 rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400 text-[10px] font-bold uppercase tracking-widest">
            Playoffs
          </span>
        </div>
      )}
      {valueBet && (
        <div className="flex justify-center mb-3">
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 text-[11px] font-bold uppercase tracking-wide">
            <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
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
              game.hasBeenPlayed ? 'text-surface-500 dark:text-surface-400' : 'text-accent-500'
            }`}
          >
            {game.hasBeenPlayed ? 'Final' : 'VS'}
          </div>
          {game.hasBeenPlayed ? (
            <div className="stat-number text-lg text-surface-900 dark:text-white">
              {game.awayTeam?.goals ?? 0}
              <span className="text-surface-400 dark:text-surface-500 mx-1">-</span>
              {game.homeTeam?.goals ?? 0}
            </div>
          ) : (
            game.gameDate && (
              <div className="text-[11px] font-mono text-surface-400 dark:text-surface-500">
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
        <div className="col-span-3 mt-2 pt-2 border-t border-surface-200/50 dark:border-white/[0.04]">
          <div className="grid grid-cols-3 items-center text-[11px] font-mono">
            {oddsType === 'moneyline' ? (
              <>
                <span className={`text-center stat-number ${oddsColor}`}>
                  {game.awayTeam ? formatOdds(game.awayTeam.modelOdds, format) : '-'}
                </span>
                <span className="text-center text-surface-400 dark:text-surface-500">
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
                <span className="text-center text-surface-400 dark:text-surface-500">
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
                <span className="text-center text-surface-400 dark:text-surface-500">
                  {getModelName(game.modelId)}
                  {game.predictedTotal != null ? ` (${game.predictedTotal.toFixed(1)})` : ''}
                </span>
                <span className={`text-center stat-number ${oddsColor}`}>{'-'}</span>
              </>
            )}
          </div>
          {game.bookmakerOdds?.length > 0 &&
            (() => {
              const pinned = ['DraftKings', 'Kalshi'];
              const shown = pinned
                .map((name) => game.bookmakerOdds.find((bm) => bm.bookmakerName === name))
                .filter(Boolean) as BookmakerOddsVM[];
              const rest = game.bookmakerOdds.filter((bm) => !pinned.includes(bm.bookmakerName));
              return (
                <>
                  {shown.map((bm) => (
                    <BookmakerOddsRow
                      key={bm.bookmakerName}
                      bm={bm}
                      oddsType={oddsType}
                      format={format}
                    />
                  ))}
                  {rest.length > 0 && (
                    <details className="mt-1">
                      <summary className="text-[10px] text-center text-surface-400 dark:text-surface-500 cursor-pointer hover:text-surface-600 dark:hover:text-surface-300 select-none">
                        {rest.length} more bookmaker{rest.length > 1 ? 's' : ''}
                      </summary>
                      {rest.map((bm) => (
                        <BookmakerOddsRow
                          key={bm.bookmakerName}
                          bm={bm}
                          oddsType={oddsType}
                          format={format}
                        />
                      ))}
                    </details>
                  )}
                </>
              );
            })()}
        </div>
      </div>
    </Link>
  );
}
