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

export function TeamDetailPage() {
  const { teamId } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  const season = Number(searchParams.get('season')) || getCurrentSeason();

  const { data: team, loading, error } = useFetch(
    () => getTeam(Number(teamId), season),
    [teamId, season]
  );

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

  if (error) return <div className="text-center text-red-500 py-8">{error}</div>;
  if (!team) return <div className="text-center text-gray-500 py-12">Team not found.</div>;

  const accuracy = team.totalGameCount > 0
    ? ((team.totalModelAccurateGameCount / team.totalGameCount) * 100).toFixed(1)
    : '0.0';

  return (
    <div>
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-6 gap-4">
        <div className="flex items-center gap-4">
          <img
            src={team.logoUri}
            alt={team.teamName}
            className="h-16 w-16 object-contain"
            onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
          />
          <div>
            <h1 className="text-2xl font-bold">{team.locationName} {team.teamName}</h1>
            <p className="text-gray-500 dark:text-gray-400">
              {team.seasonWins}-{team.seasonLosses} | Accuracy: {accuracy}% | Log Loss: {team.modelLogLoss.toFixed(4)}
            </p>
          </div>
        </div>
        <SeasonSelector
          value={season}
          onChange={(s) => setSearchParams({ season: String(s) })}
        />
      </div>

      {chartData.length > 1 && (
        <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 mb-6">
          <h2 className="text-sm font-semibold mb-3 text-gray-600 dark:text-gray-300">Cumulative Avg Log Loss</h2>
          <ResponsiveContainer width="100%" height={250}>
            <LineChart data={chartData}>
              <XAxis dataKey="date" tick={{ fontSize: 11 }} interval="preserveStartEnd" />
              <YAxis tick={{ fontSize: 11 }} domain={['auto', 'auto']} />
              <Tooltip />
              <Line type="monotone" dataKey="logLoss" stroke="#3b82f6" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {team.gameOddsVM.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b-2 border-gray-200 dark:border-gray-700 text-left">
                <th className="py-2 px-3">Date</th>
                <th className="py-2 px-3">Opponent</th>
                <th className="py-2 px-3 text-center">H/A</th>
                <th className="py-2 px-3 text-center">Odds</th>
                <th className="py-2 px-3 text-center">Score</th>
                <th className="py-2 px-3 text-center">Result</th>
                <th className="py-2 px-3 text-center">Log Loss</th>
              </tr>
            </thead>
            <tbody>
              {team.gameOddsVM.map((game) => {
                const isHome = game.homeTeam?.id === team.id;
                const opponent = isHome ? game.awayTeam : game.homeTeam;
                const teamSide = isHome ? game.homeTeam : game.awayTeam;
                const correct = game.hasBeenPlayed && wasCorrectlyPredicted(game);
                const incorrect = game.hasBeenPlayed && !wasCorrectlyPredicted(game);
                const won = game.hasBeenPlayed && (
                  (isHome && game.winner === Winner.HOME) ||
                  (!isHome && game.winner === Winner.AWAY)
                );

                return (
                  <tr
                    key={game.id}
                    className={`border-b border-gray-200 dark:border-gray-700 ${
                      correct ? 'bg-green-50 dark:bg-green-950' : incorrect ? 'bg-red-50 dark:bg-red-950' : ''
                    }`}
                  >
                    <td className="py-2 px-3">{formatShortDate(game.gameDate)}</td>
                    <td className="py-2 px-3">
                      <div className="flex items-center gap-2">
                        {opponent && (
                          <img
                            src={opponent.logoUri}
                            alt=""
                            className="h-5 w-5 object-contain"
                            onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
                          />
                        )}
                        {opponent ? `${opponent.locationName} ${opponent.teamName}` : 'TBD'}
                      </div>
                    </td>
                    <td className="py-2 px-3 text-center">{isHome ? 'H' : 'A'}</td>
                    <td className="py-2 px-3 text-center">
                      {teamSide ? `${(teamSide.modelOdds * 100).toFixed(1)}%` : '-'}
                    </td>
                    <td className="py-2 px-3 text-center">
                      {game.hasBeenPlayed
                        ? `${game.awayTeam?.goals ?? 0}-${game.homeTeam?.goals ?? 0}`
                        : '-'}
                    </td>
                    <td className="py-2 px-3 text-center font-medium">
                      {game.hasBeenPlayed ? (won ? 'W' : 'L') : '-'}
                    </td>
                    <td className="py-2 px-3 text-center">
                      {game.hasBeenPlayed ? game.logLoss.toFixed(4) : '-'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {team.gameOddsVM.length === 0 && (
        <div className="text-center text-gray-500 dark:text-gray-400 py-12">
          No games found for this team and season.
        </div>
      )}
    </div>
  );
}
