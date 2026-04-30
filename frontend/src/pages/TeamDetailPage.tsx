import { useMemo } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { useFetch } from '../hooks/useFetch';
import { getTeam } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import { formatShortDate } from '../utils/dates';
import { wasCorrectlyPredicted, calculateLogLoss } from '../utils/predictions';
import { Skeleton } from '../components/Skeleton';
import { SeasonSelector } from '../components/SeasonSelector';
import { LogLossChart } from '../components/LogLossChart';
import { Winner } from '../types';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { formatOdds } from '../utils/oddsFormat';

function StatCard({
  label,
  value,
  sub,
  accent,
}: {
  label: string;
  value: string;
  sub?: string;
  accent?: boolean;
}) {
  return (
    <div className="glass rounded-xl p-4 text-center">
      <div className={`stat-number text-2xl font-bold ${accent ? 'text-accent-500' : ''}`}>
        {value}
      </div>
      <div className="text-xs text-surface-500 dark:text-surface-400 mt-1">{label}</div>
      {sub && <div className="text-xs text-surface-400 dark:text-surface-500 mt-0.5">{sub}</div>}
    </div>
  );
}

export function TeamDetailPage() {
  const { format } = useOddsFormatContext();
  const { teamId } = useParams();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const season = Number(searchParams.get('season')) || getCurrentSeason();

  const {
    data: team,
    loading,
    error,
  } = useFetch(() => getTeam(Number(teamId), season), [teamId, season]);

  const kalshiStats = useMemo(() => {
    if (!team) return null;
    let games = 0;
    let accurate = 0;
    let totalLoss = 0;
    for (const g of team.gameOddsVM) {
      if (!g.hasBeenPlayed) continue;
      const k = g.bookmakerOdds?.find((b) => b.bookmakerName === 'Kalshi');
      if (!k || k.homeOdds <= 0 || k.awayOdds <= 0) continue;
      games++;
      const loss = calculateLogLoss(k.homeOdds, k.awayOdds, g.winner);
      totalLoss += loss;
      const kPredictedHome = k.homeOdds > k.awayOdds;
      const homeWon = g.winner === Winner.HOME;
      if (kPredictedHome === homeWon) accurate++;
    }
    return {
      games,
      accurate,
      logLoss: games > 0 ? totalLoss / games : 0,
      accuracyPct: games > 0 ? ((accurate / games) * 100).toFixed(1) : '0.0',
    };
  }, [team]);

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-10 w-1/3" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error) return <div className="glass rounded-xl text-center text-red-500 py-8">{error}</div>;
  if (!team) return <div className="text-center text-surface-400 py-12">Team not found.</div>;

  const accuracyPct =
    team.totalGameCount > 0
      ? ((team.totalModelAccurateGameCount / team.totalGameCount) * 100).toFixed(1)
      : '0.0';
  const dkAccuracyPct =
    team.draftKingsGameCount > 0
      ? ((team.draftKingsAccurateGameCount / team.draftKingsGameCount) * 100).toFixed(1)
      : '0.0';

  return (
    <div>
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-8 gap-4">
        <div className="flex items-center gap-5">
          <img
            src={team.logoUri}
            alt={team.teamName}
            className="h-20 w-20 object-contain drop-shadow-lg"
            onError={(e) => {
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
          <div>
            <h1 className="font-display text-3xl font-bold tracking-tight">
              {team.locationName} {team.teamName}
            </h1>
            <span className="stat-number text-sm text-surface-500 dark:text-surface-400">
              {team.seasonWins}-{team.seasonLosses}-{team.seasonOvertimeLosses}
            </span>
          </div>
        </div>
        <SeasonSelector value={season} onChange={(s) => setSearchParams({ season: String(s) })} />
      </div>

      <LogLossChart games={team.gameOddsVM} />

      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3 mb-8">
        <StatCard label="In-House Accuracy" value={`${accuracyPct}%`} accent />
        <StatCard
          label="In-House Log Loss"
          value={team.modelLogLoss.toFixed(4)}
          sub={`${team.totalGameCount} games`}
        />
        {team.draftKingsGameCount > 0 && (
          <>
            <StatCard label="DK Accuracy" value={`${dkAccuracyPct}%`} />
            <StatCard
              label="DK Log Loss"
              value={team.draftKingsLogLoss.toFixed(4)}
              sub={`${team.draftKingsGameCount} games`}
            />
          </>
        )}
        {kalshiStats && kalshiStats.games > 0 && (
          <>
            <StatCard label="Kalshi Accuracy" value={`${kalshiStats.accuracyPct}%`} />
            <StatCard
              label="Kalshi Log Loss"
              value={kalshiStats.logLoss.toFixed(4)}
              sub={`${kalshiStats.games} games`}
            />
          </>
        )}
      </div>

      {team.gameOddsVM.length > 0 && (
        <div className="glass rounded-xl overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs uppercase tracking-wider text-surface-400 dark:text-surface-500">
                  <th className="py-3 px-4 font-semibold">Date</th>
                  <th className="py-3 px-4 font-semibold">Opponent</th>
                  <th className="py-3 px-4 text-center font-semibold">H/A</th>
                  <th className="py-3 px-4 text-center font-semibold">Home Odds</th>
                  <th className="py-3 px-4 text-center font-semibold">DK Odds</th>
                  <th className="py-3 px-4 text-center font-semibold">Kalshi Odds</th>
                  <th className="py-3 px-4 text-center font-semibold">Score</th>
                  <th className="py-3 px-4 text-center font-semibold">Result</th>
                  <th className="py-3 px-4 text-center font-semibold">Home Loss</th>
                  <th className="py-3 px-4 text-center font-semibold">DK Loss</th>
                  <th className="py-3 px-4 text-center font-semibold">Kalshi Loss</th>
                </tr>
              </thead>
              <tbody>
                {team.gameOddsVM.map((game) => {
                  const isHome = game.homeTeam?.id === team.id;
                  const opponent = isHome ? game.awayTeam : game.homeTeam;
                  const teamSide = isHome ? game.homeTeam : game.awayTeam;
                  const dk = game.bookmakerOdds?.find((b) => b.bookmakerName === 'DraftKings');
                  const kalshi = game.bookmakerOdds?.find((b) => b.bookmakerName === 'Kalshi');
                  const dkTeamOdds = dk ? (isHome ? dk.homeOdds : dk.awayOdds) : null;
                  const kalshiTeamOdds = kalshi
                    ? isHome
                      ? kalshi.homeOdds
                      : kalshi.awayOdds
                    : null;
                  const correct = game.hasBeenPlayed && wasCorrectlyPredicted(game);
                  const incorrect = game.hasBeenPlayed && !wasCorrectlyPredicted(game);
                  const won =
                    game.hasBeenPlayed &&
                    ((isHome && game.winner === Winner.HOME) ||
                      (!isHome && game.winner === Winner.AWAY));

                  return (
                    <tr
                      key={game.id}
                      onClick={() => navigate(`/game/${game.id}`, { state: { game } })}
                      className={`border-b border-surface-100 dark:border-white/[0.03] transition-colors cursor-pointer ${
                        correct
                          ? 'bg-blue-50/50 dark:bg-blue-500/[0.04] hover:bg-blue-50 dark:hover:bg-blue-500/[0.07]'
                          : incorrect
                            ? 'bg-red-50/50 dark:bg-red-500/[0.04] hover:bg-red-50 dark:hover:bg-red-500/[0.07]'
                            : 'hover:bg-surface-50 dark:hover:bg-white/[0.02]'
                      }`}
                    >
                      <td className="py-3 px-4 font-mono text-xs">
                        {formatShortDate(game.gameDate)}
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-2.5">
                          {opponent && (
                            <img
                              src={opponent.logoUri}
                              alt=""
                              className="h-5 w-5 object-contain"
                              onError={(e) => {
                                (e.target as HTMLImageElement).style.display = 'none';
                              }}
                            />
                          )}
                          <span className="font-medium">
                            {opponent ? `${opponent.locationName} ${opponent.teamName}` : 'TBD'}
                          </span>
                        </div>
                      </td>
                      <td className="py-3 px-4 text-center">
                        <span
                          className={`inline-block px-2 py-0.5 rounded text-xs font-mono font-semibold ${
                            isHome
                              ? 'bg-accent-500/10 text-accent-500'
                              : 'bg-surface-200/50 dark:bg-white/[0.05] text-surface-500 dark:text-surface-400'
                          }`}
                        >
                          {isHome ? 'H' : 'A'}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-accent-500">
                        {teamSide ? formatOdds(teamSide.modelOdds, format) : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number">
                        {dkTeamOdds != null ? formatOdds(dkTeamOdds, format) : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number">
                        {kalshiTeamOdds != null ? formatOdds(kalshiTeamOdds, format) : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number">
                        {game.hasBeenPlayed
                          ? `${game.awayTeam?.goals ?? 0}-${game.homeTeam?.goals ?? 0}`
                          : '-'}
                      </td>
                      <td className="py-3 px-4 text-center">
                        {game.hasBeenPlayed ? (
                          <span
                            className={`inline-block px-2 py-0.5 rounded text-xs font-bold ${
                              won ? 'bg-blue-500/10 text-blue-500' : 'bg-red-500/10 text-red-500'
                            }`}
                          >
                            {won ? 'W' : 'L'}
                          </span>
                        ) : (
                          <span className="text-surface-400">-</span>
                        )}
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-xs">
                        {game.hasBeenPlayed && game.logLoss != null ? game.logLoss.toFixed(4) : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-xs">
                        {game.hasBeenPlayed && dk
                          ? calculateLogLoss(dk.homeOdds, dk.awayOdds, game.winner).toFixed(4)
                          : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-xs">
                        {game.hasBeenPlayed && kalshi && kalshi.homeOdds > 0 && kalshi.awayOdds > 0
                          ? calculateLogLoss(kalshi.homeOdds, kalshi.awayOdds, game.winner).toFixed(
                              4,
                            )
                          : '-'}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {team.gameOddsVM.length === 0 && (
        <div className="text-center text-surface-400 dark:text-surface-500 py-12">
          No games found for this team and season.
        </div>
      )}
    </div>
  );
}
