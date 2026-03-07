import type { GameOddsVM } from '../types';
import { Winner } from '../types';
import { predictionBorderClass } from '../utils/predictions';

interface GameCardProps {
  game: GameOddsVM;
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
  if (!team) return <div className="flex-1 text-center text-gray-400">TBD</div>;

  return (
    <div
      className={`flex-1 flex flex-col items-center gap-1 ${isWinner && played ? 'font-bold' : ''}`}
    >
      <img
        src={team.logoUri}
        alt={team.teamName}
        className="h-16 w-16 object-contain"
        onError={(e) => {
          (e.target as HTMLImageElement).style.display = 'none';
        }}
      />
      <div className="text-sm leading-tight">
        {team.locationName} {team.teamName}
      </div>
      <div className="flex items-baseline gap-2">
        <span className="text-base font-semibold text-blue-600 dark:text-blue-400">
          {(team.modelOdds * 100).toFixed(1)}%
        </span>
        {played && <span className="text-xl font-bold">{team.goals}</span>}
      </div>
    </div>
  );
}

export function GameCard({ game }: GameCardProps) {
  const borderClass = predictionBorderClass(game);

  return (
    <div className={`border-2 rounded-lg px-4 py-3 ${borderClass} bg-gray-50 dark:bg-gray-800`}>
      <div className="flex items-center justify-center gap-4">
        <TeamSide
          team={game.awayTeam}
          isWinner={game.hasBeenPlayed && game.winner === Winner.AWAY}
          played={game.hasBeenPlayed}
        />
        <div className="text-gray-400 text-xs font-medium">
          {game.hasBeenPlayed ? 'FINAL' : 'VS'}
        </div>
        <TeamSide
          team={game.homeTeam}
          isWinner={game.hasBeenPlayed && game.winner === Winner.HOME}
          played={game.hasBeenPlayed}
        />
      </div>
      {game.hasBeenPlayed && (
        <div className="text-center text-xs text-gray-500 dark:text-gray-400 mt-1">
          Log Loss: {game.logLoss.toFixed(4)}
        </div>
      )}
    </div>
  );
}
