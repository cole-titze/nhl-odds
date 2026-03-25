import { useNavigate } from 'react-router-dom';
import type { TeamVM } from '../types';

interface KalshiTeamStats {
  games: number;
  accurate: number;
  logLoss: number;
  accuracyPct: string;
}

interface TeamRowProps {
  team: TeamVM;
  season: number;
  showDk?: boolean;
  showKalshi?: boolean;
  kalshiStats?: KalshiTeamStats;
}

export function TeamRow({ team, season, showDk, showKalshi, kalshiStats }: TeamRowProps) {
  const navigate = useNavigate();
  const accuracy =
    team.totalGameCount > 0
      ? ((team.totalModelAccurateGameCount / team.totalGameCount) * 100).toFixed(1)
      : '0.0';
  const dkAccuracy =
    team.draftKingsGameCount > 0
      ? ((team.draftKingsAccurateGameCount / team.draftKingsGameCount) * 100).toFixed(1)
      : '-';

  return (
    <tr
      onClick={() => navigate(`/team/${team.id}?season=${season}`)}
      className="cursor-pointer table-row-hover border-b border-surface-100 dark:border-white/[0.04] group"
    >
      <td className="py-3.5 px-4">
        <div className="flex items-center gap-3">
          <img
            src={team.logoUri}
            alt={team.teamName}
            className="h-8 w-8 object-contain"
            onError={(e) => {
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
          <span className="font-medium group-hover:text-accent-500 transition-colors">
            {team.locationName} {team.teamName}
          </span>
        </div>
      </td>
      <td className="py-3.5 px-4 text-center stat-number text-sm">
        {team.seasonWins * 2 + team.seasonOvertimeLosses}
      </td>
      <td className="py-3.5 px-4 text-center stat-number text-sm">
        {team.seasonWins}-{team.seasonLosses}-{team.seasonOvertimeLosses}
      </td>
      <td className="py-3.5 px-4 text-center stat-number text-sm">{accuracy}%</td>
      <td className="py-3.5 px-4 text-center stat-number text-sm">
        {team.modelLogLoss.toFixed(4)}
      </td>
      {showDk && (
        <td className="py-3.5 px-4 text-center stat-number text-sm">
          {dkAccuracy !== '-' ? `${dkAccuracy}%` : '-'}
        </td>
      )}
      {showDk && (
        <td className="py-3.5 px-4 text-center stat-number text-sm">
          {team.draftKingsGameCount > 0 ? team.draftKingsLogLoss.toFixed(4) : '-'}
        </td>
      )}
      {showKalshi && (
        <td className="py-3.5 px-4 text-center stat-number text-sm">
          {kalshiStats && kalshiStats.games > 0 ? `${kalshiStats.accuracyPct}%` : '-'}
        </td>
      )}
      {showKalshi && (
        <td className="py-3.5 px-4 text-center stat-number text-sm">
          {kalshiStats && kalshiStats.games > 0 ? kalshiStats.logLoss.toFixed(4) : '-'}
        </td>
      )}
    </tr>
  );
}
