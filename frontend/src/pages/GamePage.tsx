import { useParams, useLocation, Link } from 'react-router-dom';
import { useState, useEffect } from 'react';
import type { GameOddsVM, BookmakerOddsVM } from '../types';
import { Winner } from '../types';
import { getGameOddsInDateRange } from '../api/gameOdds';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { formatOdds } from '../utils/oddsFormat';
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

function formatPrice(price: number): string {
  return price > 0 ? `+${price}` : `${price}`;
}

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

    setLoading(true);
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
        <Skeleton className="h-40 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error) return <div className="glass rounded-xl text-center text-red-500 py-8">{error}</div>;
  if (!game) return <div className="text-center text-surface-400 py-12">Game not found.</div>;

  const borderClass = predictionBorderClass(game);
  const correct = game.hasBeenPlayed ? wasCorrectlyPredicted(game) : null;

  return (
    <div className="max-w-2xl mx-auto space-y-6">
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
                game.hasBeenPlayed ? 'text-surface-500 dark:text-surface-400' : 'text-accent-500'
              }`}
            >
              {game.hasBeenPlayed ? 'Final' : 'VS'}
            </div>
            {game.hasBeenPlayed ? (
              <div className="stat-number text-2xl text-surface-900 dark:text-white">
                {game.awayTeam?.goals ?? 0}
                <span className="text-surface-400 dark:text-surface-500 mx-2">-</span>
                {game.homeTeam?.goals ?? 0}
              </div>
            ) : (
              game.gameDate && (
                <div className="text-xs font-mono text-surface-400 dark:text-surface-500">
                  {new Date(game.gameDate).toLocaleTimeString([], {
                    hour: 'numeric',
                    minute: '2-digit',
                  })}
                </div>
              )
            )}
            {game.gameDate && (
              <div className="text-xs text-surface-400 dark:text-surface-500 mt-1">
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
        <h2 className="text-xs font-mono font-bold tracking-widest uppercase text-surface-400 dark:text-surface-500 mb-4">
          Model Prediction
        </h2>
        <div className="grid grid-cols-3 items-center text-sm font-mono">
          <span className="text-center stat-number text-accent-500">
            {game.awayTeam ? formatOdds(game.awayTeam.modelOdds, format) : '-'}
          </span>
          <span className="text-center text-surface-400 dark:text-surface-500">
            {getModelName(game.modelId)}
          </span>
          <span className="text-center stat-number text-accent-500">
            {game.homeTeam ? formatOdds(game.homeTeam.modelOdds, format) : '-'}
          </span>
        </div>
        {game.hasBeenPlayed && (
          <div className="flex items-center justify-center gap-4 mt-3 pt-3 border-t border-surface-200/50 dark:border-white/[0.04]">
            <span
              className={`inline-block px-2.5 py-0.5 rounded text-xs font-bold ${
                correct ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'
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

      {/* Bookmaker Odds Table */}
      {game.bookmakerOdds?.length > 0 && (
        <div className="glass rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-surface-200/50 dark:border-white/[0.04] flex items-center justify-between">
            <h2 className="text-xs font-mono font-bold tracking-widest uppercase text-surface-400 dark:text-surface-500">
              Bookmaker Odds
            </h2>
            <StrategyPicker />
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs uppercase tracking-wider text-surface-400 dark:text-surface-500">
                  <th className="py-3 px-4 font-semibold">Bookmaker</th>
                  <th className="py-3 px-4 text-center font-semibold" colSpan={2}>
                    Moneyline
                  </th>
                  <th className="py-3 px-4 text-center font-semibold" colSpan={2}>
                    Spread
                  </th>
                  <th className="py-3 px-4 text-center font-semibold" colSpan={2}>
                    Over/Under
                  </th>
                  {game.hasBeenPlayed && (
                    <th className="py-3 px-4 text-center font-semibold">Log Loss</th>
                  )}
                </tr>
                <tr className="border-b border-surface-100 dark:border-white/[0.03] text-[10px] uppercase tracking-wider text-surface-300 dark:text-surface-600">
                  <th className="pb-2 px-4"></th>
                  <th className="pb-2 px-2 text-center font-medium">Away</th>
                  <th className="pb-2 px-2 text-center font-medium">Home</th>
                  <th className="pb-2 px-2 text-center font-medium">Away</th>
                  <th className="pb-2 px-2 text-center font-medium">Home</th>
                  <th className="pb-2 px-2 text-center font-medium">Over</th>
                  <th className="pb-2 px-2 text-center font-medium">Under</th>
                  {game.hasBeenPlayed && <th className="pb-2 px-2"></th>}
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
          </div>
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
  if (!team) return <div className="flex-1 text-center text-surface-400">TBD</div>;

  return (
    <div className={`flex flex-col items-center gap-2 ${isWinner && played ? 'font-bold' : ''}`}>
      <div className="relative">
        <img
          src={team.logoUri}
          alt={team.teamName}
          className="h-20 w-20 object-contain drop-shadow-lg"
          onError={(e) => {
            (e.target as HTMLImageElement).style.display = 'none';
          }}
        />
        {isWinner && played && (
          <div className="absolute -bottom-1 -right-1 w-5 h-5 rounded-full bg-accent-500 flex items-center justify-center">
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
        className="text-center leading-tight hover:text-accent-500 transition-colors"
      >
        <div className="text-xs text-surface-600 dark:text-surface-400">{team.locationName}</div>
        <div className="text-surface-900 dark:text-white text-sm font-medium">{team.teamName}</div>
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
      <td className="py-3 px-4 font-medium text-xs">
        <div className="flex items-center gap-2">
          {bm.bookmakerName}
          {valueBet && (
            <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 text-[10px] font-bold uppercase tracking-wide">
              {valueBet.side === 'home' ? game.homeTeam?.teamName : game.awayTeam?.teamName}
              {' +'}
              {(valueBet.edge * 100).toFixed(0)}%
            </span>
          )}
        </div>
      </td>
      <td
        className={`${cellClass}${valueBet?.side === 'away' ? ' text-emerald-600 dark:text-emerald-400 font-bold' : ''}`}
      >
        {formatOdds(bm.awayOdds, format)}
      </td>
      <td
        className={`${cellClass}${valueBet?.side === 'home' ? ' text-emerald-600 dark:text-emerald-400 font-bold' : ''}`}
      >
        {formatOdds(bm.homeOdds, format)}
      </td>
      <td className={cellClass}>
        {bm.awayPoint ? `${formatPoint(bm.awayPoint)} (${formatPrice(bm.awayPrice)})` : '-'}
      </td>
      <td className={cellClass}>
        {bm.homePoint ? `${formatPoint(bm.homePoint)} (${formatPrice(bm.homePrice)})` : '-'}
      </td>
      <td className={cellClass}>
        {bm.overPrice ? `O ${bm.overUnderPoint} (${formatPrice(bm.overPrice)})` : '-'}
      </td>
      <td className={cellClass}>
        {bm.underPrice ? `U ${bm.overUnderPoint} (${formatPrice(bm.underPrice)})` : '-'}
      </td>
      {game.hasBeenPlayed && (
        <td className={cellClass}>
          {bmLogLoss != null && bmLogLoss >= 0 ? bmLogLoss.toFixed(4) : '-'}
        </td>
      )}
    </tr>
  );
}
