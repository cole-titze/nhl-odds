import { useParams, useSearchParams } from 'react-router-dom';
import { useMemo } from 'react';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import { useFetch } from '../hooks/useFetch';
import { getTeam } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import { formatShortDate } from '../utils/dates';
import { wasCorrectlyPredicted } from '../utils/predictions';
import { Skeleton } from '../components/Skeleton';
import { SeasonSelector } from '../components/SeasonSelector';
import { Winner } from '../types';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { formatOdds } from '../utils/oddsFormat';

export function TeamDetailPage() {
  const { format } = useOddsFormatContext();
  const { teamId } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  const season = Number(searchParams.get('season')) || getCurrentSeason();

  const {
    data: team,
    loading,
    error,
  } = useFetch(() => getTeam(Number(teamId), season), [teamId, season]);

  const chartData = useMemo(() => {
    if (!team) return [];
    let cumLogLoss = 0;
    let count = 0;
    return team.gameOddsVM
      .filter((g) => g.hasBeenPlayed)
      .map((g) => {
        cumLogLoss += g.logLoss;
        count++;
        return {
          date: formatShortDate(g.gameDate),
          logLoss: +(cumLogLoss / count).toFixed(4),
        };
      });
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
            <div className="flex items-center gap-4 mt-1.5">
              <span className="stat-number text-sm text-surface-500 dark:text-surface-400">
                {team.seasonWins}-{team.seasonLosses}
              </span>
              <span className="w-1 h-1 rounded-full bg-surface-300 dark:bg-surface-600" />
              <span className="stat-number text-sm text-accent-500">{accuracyPct}% accuracy</span>
              <span className="w-1 h-1 rounded-full bg-surface-300 dark:bg-surface-600" />
              <span className="stat-number text-sm text-surface-500 dark:text-surface-400">
                {team.modelLogLoss.toFixed(4)} log loss
              </span>
            </div>
          </div>
        </div>
        <SeasonSelector value={season} onChange={(s) => setSearchParams({ season: String(s) })} />
      </div>

      {chartData.length > 1 && (
        <div className="glass rounded-xl p-5 mb-8">
          <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500 mb-4">
            Cumulative Avg Log Loss
          </h2>
          <ResponsiveContainer width="100%" height={250}>
            <LineChart data={chartData}>
              <XAxis
                dataKey="date"
                tick={{ fontSize: 11, fontFamily: 'JetBrains Mono' }}
                interval="preserveStartEnd"
                stroke="#525252"
                tickLine={false}
                axisLine={false}
              />
              <YAxis
                tick={{ fontSize: 11, fontFamily: 'JetBrains Mono' }}
                domain={['auto', 'auto']}
                stroke="#525252"
                tickLine={false}
                axisLine={false}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: 'rgba(10, 10, 10, 0.9)',
                  border: '1px solid rgba(255,255,255,0.08)',
                  borderRadius: '8px',
                  fontFamily: 'JetBrains Mono',
                  fontSize: '12px',
                  color: '#fff',
                  backdropFilter: 'blur(12px)',
                }}
              />
              <Line
                type="monotone"
                dataKey="logLoss"
                stroke="#3b82f6"
                strokeWidth={2.5}
                dot={false}
                activeDot={{ r: 4, fill: '#3b82f6', stroke: '#141418', strokeWidth: 2 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {team.gameOddsVM.length > 0 && (
        <div className="glass rounded-xl overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs uppercase tracking-wider text-surface-400 dark:text-surface-500">
                  <th className="py-3 px-4 font-semibold">Date</th>
                  <th className="py-3 px-4 font-semibold">Opponent</th>
                  <th className="py-3 px-4 text-center font-semibold">H/A</th>
                  <th className="py-3 px-4 text-center font-semibold">Odds</th>
                  <th className="py-3 px-4 text-center font-semibold">Score</th>
                  <th className="py-3 px-4 text-center font-semibold">Result</th>
                  <th className="py-3 px-4 text-center font-semibold">Log Loss</th>
                </tr>
              </thead>
              <tbody>
                {team.gameOddsVM.map((game) => {
                  const isHome = game.homeTeam?.id === team.id;
                  const opponent = isHome ? game.awayTeam : game.homeTeam;
                  const teamSide = isHome ? game.homeTeam : game.awayTeam;
                  const correct = game.hasBeenPlayed && wasCorrectlyPredicted(game);
                  const incorrect = game.hasBeenPlayed && !wasCorrectlyPredicted(game);
                  const won =
                    game.hasBeenPlayed &&
                    ((isHome && game.winner === Winner.HOME) ||
                      (!isHome && game.winner === Winner.AWAY));

                  return (
                    <tr
                      key={game.id}
                      className={`border-b border-surface-100 dark:border-white/[0.03] transition-colors ${
                        correct
                          ? 'bg-blue-50/50 dark:bg-blue-500/[0.04]'
                          : incorrect
                            ? 'bg-red-50/50 dark:bg-red-500/[0.04]'
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
                        {game.hasBeenPlayed
                          ? `${game.awayTeam?.goals ?? 0}-${game.homeTeam?.goals ?? 0}`
                          : '-'}
                      </td>
                      <td className="py-3 px-4 text-center">
                        {game.hasBeenPlayed ? (
                          <span
                            className={`inline-block px-2 py-0.5 rounded text-xs font-bold ${
                              won
                                ? 'bg-blue-500/10 text-blue-500'
                                : 'bg-red-500/10 text-red-500'
                            }`}
                          >
                            {won ? 'W' : 'L'}
                          </span>
                        ) : (
                          <span className="text-surface-400">-</span>
                        )}
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-xs">
                        {game.hasBeenPlayed ? game.logLoss.toFixed(4) : '-'}
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
