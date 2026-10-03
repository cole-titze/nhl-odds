import { useParams, useLocation, Link } from 'react-router-dom';
import { useState, useEffect } from 'react';
import type { GameOddsVM, BookmakerOddsVM } from '../types';
import { Winner } from '../types';
import { getGameOddsInDateRange } from '../api/gameOdds';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { formatOdds, formatAmericanOdds } from '../utils/oddsFormat';
import { getModelName } from '../utils/modelNames';
import {
  wasCorrectlyPredicted,
  calculateLogLoss,
  predictionBorderClass,
} from '../utils/predictions';
import { checkStrategy } from '../utils/bettingStrategies';
import { useStrategy } from '../contexts/StrategyContext';
import { StrategyPicker } from '../components/StrategyPicker';
import { Skeleton } from '../components/Skeleton';
import { TeamLogo } from '../components/TeamLogo';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';

function formatPoint(point: number): string {
  return point > 0 ? `+${point}` : `${point}`;
}

export function GamePage() {
  const { gameId } = useParams();
  const location = useLocation();
  const { format } = useOddsFormatContext();
  const { strategy } = useStrategy();
  const [game, setGame] = useState<GameOddsVM | null>(
    (location.state as { game?: GameOddsVM } | null)?.game ?? null,
  );
  const [loading, setLoading] = useState(!game);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (game) return;
    if (!gameId) return;

    const seasonStartYear = Number(gameId.toString().slice(0, 4));
    // Fetch entire season date range to find this game
    const startDate = `${seasonStartYear}-09-01`;
    const endDate = `${seasonStartYear + 1}-07-31`;

    getGameOddsInDateRange(startDate, endDate, seasonStartYear)
      .then((games) => {
        const found = games.find((g) => g.id === Number(gameId));
        if (found) {
          setGame(found);
        } else {
          setError('Game not found.');
        }
      })
      .catch(() => setError('Failed to load game data.'))
      .finally(() => setLoading(false));
  }, [gameId, game]);

  if (loading) {
    return (
      <div className="space-y-4 max-w-2xl mx-auto">
        <PageTitle title="Game" />
        <LoadingStatus label="Loading game…" />
        <Skeleton className="h-40 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error)
    return (
      <div
        role="alert"
        className="glass rounded-xl text-center text-red-700 dark:text-red-400 py-8"
      >
        {error}
      </div>
    );
  if (!game)
    return (
      <div className="text-center text-surface-500 dark:text-surface-400 py-12">
        Game not found.
      </div>
    );

  const borderClass = predictionBorderClass(game);
  const correct = game.hasBeenPlayed ? wasCorrectlyPredicted(game) : null;
  const matchup = `${game.awayTeam ? `${game.awayTeam.locationName} ${game.awayTeam.teamName}` : 'TBD'} at ${
    game.homeTeam ? `${game.homeTeam.locationName} ${game.homeTeam.teamName}` : 'TBD'
  }`;

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <PageTitle title={matchup} />
      <h1 className="sr-only">{matchup}</h1>
      {/* Matchup Header */}
      <div className={`glass rounded-xl px-6 py-6 ${borderClass}`}>
        <div className="grid grid-cols-3 items-center">
          <TeamHeader
            team={game.awayTeam}
            isWinner={game.hasBeenPlayed && game.winner === Winner.AWAY}
            played={game.hasBeenPlayed}
          />
          <div className="flex flex-col items-center gap-1">
            <div
              className={`text-xs font-mono font-bold tracking-widest uppercase ${
                game.hasBeenPlayed
                  ? 'text-surface-500 dark:text-surface-400'
                  : 'text-accent-600 dark:text-accent-400'
              }`}
            >
              {game.hasBeenPlayed ? 'Final' : 'VS'}
            </div>
            {game.hasBeenPlayed ? (
              <div className="stat-number text-2xl text-surface-900 dark:text-white">
                {game.awayTeam?.goals ?? 0}
                <span className="text-surface-500 dark:text-surface-400 mx-2">-</span>
                {game.homeTeam?.goals ?? 0}
              </div>
            ) : (
              game.gameDate && (
                <div className="text-xs font-mono text-surface-500 dark:text-surface-400">
                  {new Date(game.gameDate).toLocaleTimeString([], {
                    hour: 'numeric',
                    minute: '2-digit',
                  })}
                </div>
              )
            )}
            {game.gameDate && (
              <div className="text-xs text-surface-500 dark:text-surface-400 mt-1">
                {new Date(game.gameDate).toLocaleDateString([], {
                  weekday: 'short',
                  month: 'short',
                  day: 'numeric',
                  year: 'numeric',
                })}
              </div>
            )}
          </div>
          <TeamHeader
            team={game.homeTeam}
            isWinner={game.hasBeenPlayed && game.winner === Winner.HOME}
            played={game.hasBeenPlayed}
          />
        </div>
      </div>

      {/* Model Prediction */}
      <div className="glass rounded-xl px-6 py-5">
        <h2 className="text-xs font-mono font-bold tracking-widest uppercase text-surface-500 dark:text-surface-400 mb-4">
          Model Prediction
        </h2>
        <div className="grid grid-cols-3 items-center text-sm font-mono">
          <span className="text-center stat-number text-accent-600 dark:text-accent-400">
            {game.awayTeam ? formatOdds(game.awayTeam.modelOdds, format) : '-'}
          </span>
          <span className="text-center text-surface-500 dark:text-surface-400">
            {getModelName(game.modelId)}
          </span>
          <span className="text-center stat-number text-accent-600 dark:text-accent-400">
            {game.homeTeam ? formatOdds(game.homeTeam.modelOdds, format) : '-'}
          </span>
        </div>
        {game.hasBeenPlayed && game.logLoss != null && (
          <div className="flex items-center justify-center gap-4 mt-3 pt-3 border-t border-surface-200/50 dark:border-white/[0.04]">
            <span
              className={`inline-block px-2.5 py-0.5 rounded text-xs font-bold ${
                correct
                  ? 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-400'
                  : 'bg-red-500/10 text-red-700 dark:text-red-400'
              }`}
            >
              {correct ? 'Correct' : 'Incorrect'}
            </span>
            <span className="stat-number text-xs text-surface-500 dark:text-surface-400">
              Log Loss: {game.logLoss.toFixed(4)}
            </span>
          </div>
        )}
      </div>

      {/* Spread & Total Predictions */}
      {(game.predictedSpread != null || game.predictedTotal != null) && (
        <div className="glass rounded-xl px-6 py-5">
          <h2 className="text-xs font-mono font-bold tracking-widest uppercase text-surface-500 dark:text-surface-400 mb-4">
            Spread & Total Predictions
          </h2>
          <div className="grid grid-cols-2 gap-4 text-sm">
            {game.predictedSpread != null && (
              <div className="text-center">
                <div className="text-surface-500 dark:text-surface-400 text-xs mb-1">
                  Predicted Spread
                </div>
                <div className="stat-number text-lg text-accent-600 dark:text-accent-400">
                  {game.predictedSpread > 0 ? '+' : ''}
                  {game.predictedSpread.toFixed(2)}
                </div>
                {game.spreadCoverProb != null && (
                  <div className="text-xs text-surface-500 dark:text-surface-400 mt-1">
                    Cover: {(game.spreadCoverProb * 100).toFixed(0)}%
                  </div>
                )}
              </div>
            )}
            {game.predictedTotal != null && (
              <div className="text-center">
                <div className="text-surface-500 dark:text-surface-400 text-xs mb-1">
                  Predicted Total
                </div>
                <div className="stat-number text-lg text-accent-600 dark:text-accent-400">
                  {game.predictedTotal.toFixed(1)}
                </div>
                {game.totalOverProb != null && (
                  <div className="text-xs text-surface-500 dark:text-surface-400 mt-1">
                    Over: {(game.totalOverProb * 100).toFixed(0)}%
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Bookmaker Odds Table */}
      {game.bookmakerOdds?.length > 0 && (
        <div className="glass rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-surface-200/50 dark:border-white/[0.04] flex items-center justify-between">
            <h2 className="text-xs font-mono font-bold tracking-widest uppercase text-surface-500 dark:text-surface-400">
              Bookmaker Odds
            </h2>
            <StrategyPicker />
          </div>
          <ScrollRegion label="Bookmaker odds">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs uppercase tracking-wider text-surface-500 dark:text-surface-400">
                  <th scope="col" className="py-3 px-4 font-semibold">
                    Bookmaker
                  </th>
                  <th scope="colgroup" className="py-3 px-4 text-center font-semibold" colSpan={2}>
                    Moneyline
                  </th>
                  <th scope="colgroup" className="py-3 px-4 text-center font-semibold" colSpan={2}>
                    Spread
                  </th>
                  <th scope="colgroup" className="py-3 px-4 text-center font-semibold" colSpan={2}>
                    Over/Under
                  </th>
                  {game.hasBeenPlayed && (
                    <th scope="col" className="py-3 px-4 text-center font-semibold">
                      Log Loss
                    </th>
                  )}
                </tr>
                <tr className="border-b border-surface-100 dark:border-white/[0.03] text-[10px] uppercase tracking-wider text-surface-500 dark:text-surface-400">
                  <td className="pb-2 px-4" />
                  <th scope="col" className="pb-2 px-2 text-center font-medium">
                    Away
                  </th>
                  <th scope="col" className="pb-2 px-2 text-center font-medium">
                    Home
                  </th>
                  <th scope="col" className="pb-2 px-2 text-center font-medium">
                    Away
                  </th>
                  <th scope="col" className="pb-2 px-2 text-center font-medium">
                    Home
                  </th>
                  <th scope="col" className="pb-2 px-2 text-center font-medium">
                    Over
                  </th>
                  <th scope="col" className="pb-2 px-2 text-center font-medium">
                    Under
                  </th>
                  {game.hasBeenPlayed && <td className="pb-2 px-2" />}
                </tr>
              </thead>
              <tbody>
                {game.bookmakerOdds.map((bm) => (
                  <BookmakerRow
                    key={bm.bookmakerName}
                    bm={bm}
                    game={game}
                    format={format}
                    strategy={strategy}
                  />
                ))}
              </tbody>
            </table>
          </ScrollRegion>
        </div>
      )}
    </div>
  );
}

function TeamHeader({
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
    <div className={`flex flex-col items-center gap-2 ${isWinner && played ? 'font-bold' : ''}`}>
      <div className="relative">
        <TeamLogo src={team.logoUri} alt="" className="h-20 w-20 object-contain drop-shadow-lg" />
        {isWinner && played && (
          <div
            aria-hidden="true"
            className="absolute -bottom-1 -right-1 w-5 h-5 rounded-full bg-accent-500 flex items-center justify-center"
          >
            <svg
              className="w-3 h-3 text-white"
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
      <Link
        to={`/team/${team.id}`}
        className="text-center leading-tight hover:text-accent-600 dark:hover:text-accent-400 transition-colors"
      >
        <div className="text-xs text-surface-600 dark:text-surface-400">{team.locationName}</div>
        <div className="text-surface-900 dark:text-white text-sm font-medium">{team.teamName}</div>
        {isWinner && played && <span className="sr-only"> (winner)</span>}
      </Link>
    </div>
  );
}

function BookmakerRow({
  bm,
  game,
  format,
  strategy,
}: {
  bm: BookmakerOddsVM;
  game: GameOddsVM;
  format: Parameters<typeof formatOdds>[1];
  strategy: import('../utils/bettingStrategies').StrategyConfig;
}) {
  const cellClass =
    'py-3 px-2 text-center stat-number text-surface-600 dark:text-surface-400 font-mono text-xs';
  const bmLogLoss = game.hasBeenPlayed
    ? calculateLogLoss(bm.homeOdds, bm.awayOdds, game.winner)
    : null;
  const valueBet = checkStrategy(game, bm, strategy);

  return (
    <tr className="border-b border-surface-100 dark:border-white/[0.03] hover:bg-surface-50 dark:hover:bg-white/[0.02] transition-colors">
      <th scope="row" className="py-3 px-4 font-medium text-xs text-left">
        <div className="flex items-center gap-2">
          {bm.bookmakerName}
          {valueBet && (
            <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-emerald-500/10 text-emerald-700 dark:text-emerald-400 text-[10px] font-bold uppercase tracking-wide">
              {valueBet.side === 'home' ? game.homeTeam?.teamName : game.awayTeam?.teamName}
              {' +'}
              {(valueBet.edge * 100).toFixed(0)}%<span className="sr-only"> suggested bet</span>
            </span>
          )}
        </div>
      </th>
      <td
        className={`${cellClass}${valueBet?.side === 'away' ? ' text-emerald-700 dark:text-emerald-400 font-bold' : ''}`}
      >
        {formatOdds(bm.awayOdds, format)}
      </td>
      <td
        className={`${cellClass}${valueBet?.side === 'home' ? ' text-emerald-700 dark:text-emerald-400 font-bold' : ''}`}
      >
        {formatOdds(bm.homeOdds, format)}
      </td>
      <td className={cellClass}>
        {bm.awayPoint
          ? `${formatPoint(bm.awayPoint)} (${formatAmericanOdds(bm.awayPrice, format)})`
          : '-'}
      </td>
      <td className={cellClass}>
        {bm.homePoint
          ? `${formatPoint(bm.homePoint)} (${formatAmericanOdds(bm.homePrice, format)})`
          : '-'}
      </td>
      <td className={cellClass}>
        {bm.overPrice
          ? `O ${bm.overUnderPoint} (${formatAmericanOdds(bm.overPrice, format)})`
          : '-'}
      </td>
      <td className={cellClass}>
        {bm.underPrice
          ? `U ${bm.overUnderPoint} (${formatAmericanOdds(bm.underPrice, format)})`
          : '-'}
      </td>
      {game.hasBeenPlayed && (
        <td className={cellClass}>
          {bmLogLoss != null && bmLogLoss >= 0 ? bmLogLoss.toFixed(4) : '-'}
        </td>
      )}
    </tr>
  );
}
