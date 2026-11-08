import {
  data,
  useParams,
  useLocation,
  Link,
  useLoaderData,
  type LoaderFunctionArgs,
} from 'react-router';
import { useState, useEffect } from 'react';
import type {
  GameOddsVM,
  BookmakerOddsVM,
  GamePlayerVM,
  GamePlayersVM,
  PredictionFactorsVM,
} from '../types';
import { Winner } from '../types';
import { getGame, getPredictionFactors } from '../api/gameOdds';
import { getGamePlayers } from '../api/players';
import { useFetch } from '../hooks/useFetch';
import { forwardClientIp } from '../api/client';
import { useDisplayTimeZone } from '../hooks/useDisplayTimeZone';
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
import { GameLineups } from '../components/GameLineups';
import { OddsMovementChart } from '../components/OddsMovementChart';
import { TeamLogo } from '../components/TeamLogo';
import { PredictionFactors } from '../components/PredictionFactors';
import { SectionNav } from '../components/SectionNav';
import { sectionProps, type PageSection } from '../utils/sections';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';
import { breadcrumbs, sportsEvent } from '../utils/structuredData';
import { Breadcrumbs, type Crumb } from '../components/Breadcrumbs';
import {
  formatSavePct,
  formatToi,
  playerName,
  playerPath,
  positionGroups,
  positionName,
} from '../utils/players';

// Describes the link preview image (og/cards.tsx GameCard). No date: the page's
// time zone differs between the server and the browser.
function shareImageAlt(game: GameOddsVM, matchup: string): string {
  const { awayTeam: away, homeTeam: home } = game;
  if (!away || !home) return matchup;
  const odds =
    home.modelOdds != null && away.modelOdds != null
      ? `the model gives the ${away.teamName} ${Math.round(away.modelOdds * 100)}% and the ${home.teamName} ${Math.round(home.modelOdds * 100)}%`
      : 'no model odds yet';
  return game.hasBeenPlayed
    ? `${matchup}, final score ${away.goals}-${home.goals}; ${odds.replace('gives', 'gave')}`
    : `${matchup}: ${odds}`;
}

function formatPoint(point: number): string {
  return point > 0 ? `+${point}` : `${point}`;
}

// First load renders on the server; client-side navigation fetches in the page
// as before (usually not even that: game cards pass the game in location.state)
export async function loader({ params, request }: LoaderFunctionArgs) {
  const init = forwardClientIp(request);
  const playersRequest = getGamePlayers(params.gameId!, init).catch(() => null);
  let game: GameOddsVM | undefined;
  let factors: PredictionFactorsVM | null = null;
  try {
    // The prediction factors come with the first render, so their section doesn't push
    // the rest of the page down when it arrives (a layout shift)
    [game, factors] = await Promise.all([
      getGame(params.gameId!, init),
      /^\d+$/.test(params.gameId!)
        ? getPredictionFactors(Number(params.gameId), init).catch(() => null)
        : null,
    ]);
  } catch {
    return null;
  }
  const players = (await playersRequest) ?? null;
  // The page still renders its "Game not found" message, with a real 404 status for crawlers
  return game
    ? { game, factors, players }
    : data({ game: null, factors: null, players: null }, { status: 404 });
}

export async function clientLoader() {
  return null;
}

export function GamePage() {
  const loaded = useLoaderData<typeof loader>();
  const { gameId } = useParams();
  const location = useLocation();
  const { format } = useOddsFormatContext();
  const { strategy } = useStrategy();
  const { locales, timeZone } = useDisplayTimeZone();
  const [game, setGame] = useState<GameOddsVM | null>(
    (location.state as { game?: GameOddsVM } | null)?.game ?? loaded?.game ?? null,
  );
  const [loading, setLoading] = useState(!game && !loaded);
  const [error, setError] = useState<string | null>(
    loaded && !loaded.game ? 'Game not found.' : null,
  );
  // Rosters or the box score; the page works without them
  const { data: players } = useFetch(
    async () => (await getGamePlayers(gameId ?? '')) ?? null,
    [gameId],
    loaded?.players,
  );

  useEffect(() => {
    if (game || error) return;
    if (!gameId) return;

    getGame(gameId)
      .then((found) => {
        if (found) {
          setGame(found);
        } else {
          setError('Game not found.');
        }
      })
      .catch(() => setError('Failed to load game data.'))
      .finally(() => setLoading(false));
  }, [gameId, game, error]);

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

  // A title of their own, kept out of search, so these never leave the tab blank
  if (error)
    return (
      <>
        <PageTitle title={error === 'Game not found.' ? 'Game not found' : 'Game'} noindex />
        <div
          role="alert"
          className="glass rounded-xl text-center text-red-700 dark:text-red-400 py-8"
        >
          {error}
        </div>
      </>
    );
  if (!game)
    return (
      <div className="text-center text-surface-500 dark:text-surface-400 py-12">
        <PageTitle title="Game not found" noindex />
        Game not found.
      </div>
    );

  const borderClass = predictionBorderClass(game);
  const correct = game.hasBeenPlayed ? wasCorrectlyPredicted(game) : null;
  const matchup = `${game.awayTeam ? `${game.awayTeam.locationName} ${game.awayTeam.teamName}` : 'TBD'} at ${
    game.homeTeam ? `${game.homeTeam.locationName} ${game.homeTeam.teamName}` : 'TBD'
  }`;

  const crumbs: Crumb[] = [
    { name: 'Games', path: '/' },
    { name: matchup, path: `/game/${game.id}` },
  ];
  const gamePlayers = players && players.gameId === game.id ? players : null;
  const hasPredictions = game.predictedSpread != null || game.predictedTotal != null;
  const hasPlayers = !!gamePlayers && (gamePlayers.away.length > 0 || gamePlayers.home.length > 0);
  const sections: PageSection[] = [
    { id: 'prediction', label: 'Prediction' },
    ...(hasPredictions ? [{ id: 'spread-total', label: 'Spread & total' }] : []),
    ...(game.bookmakerOdds?.length > 0 ? [{ id: 'odds', label: 'Odds' }] : []),
    ...(hasPlayers
      ? [{ id: 'players', label: gamePlayers!.hasStats ? 'Box score' : 'Rosters' }]
      : []),
  ];

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <PageTitle
        title={matchup}
        description={`${matchup}: the NHL Odds model's win probability compared with sportsbook and Kalshi odds.`}
        schema={[sportsEvent(game, gamePlayers), breadcrumbs(crumbs)]}
        imageAlt={shareImageAlt(game, matchup)}
      />
      <div className="-mb-2">
        <Breadcrumbs items={crumbs} />
      </div>
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
                  {new Date(game.gameDate).toLocaleTimeString(locales, {
                    hour: 'numeric',
                    minute: '2-digit',
                    timeZone,
                  })}
                </div>
              )
            )}
            {game.gameDate && (
              <div className="text-xs text-surface-500 dark:text-surface-400 mt-1">
                {new Date(game.gameDate).toLocaleDateString(locales, {
                  weekday: 'short',
                  month: 'short',
                  day: 'numeric',
                  year: 'numeric',
                  timeZone,
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

      <SectionNav sections={sections} column="max-w-[calc(42rem+2.5rem)]" />

      {/* Model Prediction */}
      <div {...sectionProps('prediction')} className="glass rounded-xl px-6 py-5">
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

      <PredictionFactors
        key={game.id}
        game={game}
        initial={loaded?.game?.id === game.id ? loaded.factors : undefined}
      />

      {/* Spread & Total Predictions */}
      {hasPredictions && (
        <div {...sectionProps('spread-total')} className="glass rounded-xl px-6 py-5">
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
        <div {...sectionProps('odds')} className="glass rounded-xl overflow-hidden">
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

      <OddsMovementChart game={game} />
      <GameLineups game={game} />
      {hasPlayers && <GamePlayers game={game} players={gamePlayers!} />}
    </div>
  );
}

// Each team's players, linked to their pages: the box score once played, else the rosters
function GamePlayers({ game, players }: { game: GameOddsVM; players: GamePlayersVM }) {
  const sides = [
    { team: game.awayTeam, list: players.away },
    { team: game.homeTeam, list: players.home },
  ].filter((s) => s.team && s.list.length > 0);
  return (
    <section {...sectionProps('players')} aria-labelledby="players-heading" className="space-y-4">
      <h2
        id="players-heading"
        className="text-xs font-mono font-bold tracking-widest uppercase text-surface-500 dark:text-surface-400"
      >
        {players.hasStats ? 'Box Score' : 'Rosters'}
      </h2>
      {sides.map(({ team, list }) => {
        const teamName = `${team!.locationName} ${team!.teamName}`;
        return (
          <div key={team!.id} className="glass rounded-xl overflow-hidden">
            <h3 className="px-4 pt-4 pb-2 flex items-center gap-2 text-sm font-semibold">
              <TeamLogo src={team!.logoUri} alt="" className="h-5 w-5 object-contain" />
              {teamName}
            </h3>
            {players.hasStats ? (
              <BoxScore teamName={teamName} list={list} />
            ) : (
              <div className="px-4 pb-4 space-y-3">
                {positionGroups(list).map((group) => (
                  <div key={group.label}>
                    <h4 className="text-xs text-surface-500 dark:text-surface-400 mb-1">
                      {group.label}
                    </h4>
                    <ul className="grid grid-cols-1 sm:grid-cols-2 gap-x-4">
                      {group.players.map((p) => (
                        <li key={p.id} className="flex items-center gap-2 text-sm">
                          <span className="stat-number w-7 text-right text-xs text-surface-500 dark:text-surface-400">
                            {p.sweaterNumber != null ? `#${p.sweaterNumber}` : ''}
                          </span>
                          <PlayerLink player={p} />
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            )}
          </div>
        );
      })}
    </section>
  );
}

function PlayerLink({ player }: { player: GamePlayerVM }) {
  return (
    <Link
      to={playerPath(player.id)}
      className="inline-block py-0.5 font-medium hover:text-accent-600 dark:hover:text-accent-400 hover:underline"
    >
      {playerName(player)}
    </Link>
  );
}

function BoxScore({ teamName, list }: { teamName: string; list: GamePlayerVM[] }) {
  const skaters = list.filter((p) => p.position !== 'G');
  const goalies = list.filter((p) => p.position === 'G');
  const th = 'py-2 px-2 font-semibold text-center';
  const td = 'py-1.5 px-2 text-center stat-number text-xs';
  const head =
    'border-b border-surface-200 dark:border-white/[0.06] text-xs uppercase tracking-wider text-surface-500 dark:text-surface-400';
  const row = 'border-b border-surface-100 dark:border-white/[0.03]';
  const abbr = (short: string, title: string) => (
    <abbr title={title} className="no-underline">
      {short}
    </abbr>
  );
  return (
    <ScrollRegion label={`${teamName} box score`}>
      {skaters.length > 0 && (
        <table className="w-full text-sm">
          <caption className="sr-only">{teamName} skaters</caption>
          <thead>
            <tr className={head}>
              <th scope="col" className="py-2 px-4 font-semibold text-left">
                Skater
              </th>
              <th scope="col" className={th}>
                {abbr('Pos', 'Position')}
              </th>
              <th scope="col" className={th}>
                {abbr('G', 'Goals')}
              </th>
              <th scope="col" className={th}>
                {abbr('A', 'Assists')}
              </th>
              <th scope="col" className={th}>
                {abbr('SOG', 'Shots on goal')}
              </th>
              <th scope="col" className={th}>
                {abbr('TOI', 'Time on ice')}
              </th>
            </tr>
          </thead>
          <tbody>
            {skaters.map((p) => (
              <tr key={p.id} className={row}>
                <th scope="row" className="py-1.5 px-4 text-left font-normal whitespace-nowrap">
                  <span className="stat-number inline-block w-7 text-xs text-surface-500 dark:text-surface-400">
                    {p.sweaterNumber ?? ''}
                  </span>
                  <PlayerLink player={p} />
                </th>
                <td className={td}>
                  <abbr title={positionName(p.position)} className="no-underline">
                    {p.position}
                  </abbr>
                </td>
                <td className={td}>{p.stats?.goals ?? '-'}</td>
                <td className={td}>{p.stats?.assists ?? '-'}</td>
                <td className={td}>{p.stats?.shotsOnGoal ?? '-'}</td>
                <td className={td}>{p.stats ? formatToi(p.stats.timeOnIceSeconds) : '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {goalies.length > 0 && (
        <table className="w-full text-sm">
          <caption className="sr-only">{teamName} goalies</caption>
          <thead>
            <tr className={head}>
              <th scope="col" className="py-2 px-4 font-semibold text-left">
                Goalie
              </th>
              <th scope="col" className={th}>
                {abbr('SA', 'Shots against')}
              </th>
              <th scope="col" className={th}>
                {abbr('SV', 'Saves')}
              </th>
              <th scope="col" className={th}>
                {abbr('SV%', 'Save percentage')}
              </th>
              <th scope="col" className={th}>
                {abbr('TOI', 'Time on ice')}
              </th>
            </tr>
          </thead>
          <tbody>
            {goalies.map((p) => (
              <tr key={p.id} className={row}>
                <th scope="row" className="py-1.5 px-4 text-left font-normal whitespace-nowrap">
                  <span className="stat-number inline-block w-7 text-xs text-surface-500 dark:text-surface-400">
                    {p.sweaterNumber ?? ''}
                  </span>
                  <PlayerLink player={p} />
                </th>
                <td className={td}>{p.stats?.shotsAgainst ?? '-'}</td>
                <td className={td}>{p.stats?.saves ?? '-'}</td>
                <td className={td}>{formatSavePct(p.stats?.savePct ?? null)}</td>
                <td className={td}>{p.stats ? formatToi(p.stats.timeOnIceSeconds) : '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </ScrollRegion>
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

export default GamePage;
