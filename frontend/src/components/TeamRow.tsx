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
      className="cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700 border-b border-gray-200 dark:border-gray-700"
    >
      <td className="py-3 px-4">
        <div className="flex items-center gap-3">
          <img
            src={team.logoUri}
            alt={team.teamName}
            className="h-8 w-8 object-contain"
            onError={(e) => {
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
          <span>
            {team.locationName} {team.teamName}
          </span>
        </div>
      </td>
      <td className="py-3 px-4 text-center">
        {team.seasonWins}-{team.seasonLosses}
      </td>
      <td className="py-3 px-4 text-center">{accuracy}%</td>
      <td className="py-3 px-4 text-center">{team.modelLogLoss.toFixed(4)}</td>
    </tr>
  );
}
