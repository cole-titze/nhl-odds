import { useNavigate } from 'react-router-dom';
import type { TeamVM } from '../types';

interface TeamRowProps {
  team: TeamVM;
  season: number;
}

export function TeamRow({ team, season }: TeamRowProps) {
  const navigate = useNavigate();
  const accuracy =
    team.totalGameCount > 0
      ? ((team.totalModelAccurateGameCount / team.totalGameCount) * 100).toFixed(1)
      : '0.0';

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
      <td className="py-3.5 px-4 text-center stat-number text-sm text-accent-500">{accuracy}%</td>
      <td className="py-3.5 px-4 text-center stat-number text-sm">
        {team.modelLogLoss.toFixed(4)}
      </td>
    </tr>
  );
}
