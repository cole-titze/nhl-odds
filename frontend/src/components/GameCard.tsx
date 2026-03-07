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
  if (!team) return <div className="flex-1 text-center text-surface-400">TBD</div>;

  return (
    <div
      className={`flex-1 flex flex-col items-center gap-2 ${isWinner && played ? 'font-bold' : ''}`}
    >
      <div className="relative">
        <img
          src={team.logoUri}
          alt={team.teamName}
          className="h-14 w-14 object-contain drop-shadow-lg"
          onError={(e) => {
            (e.target as HTMLImageElement).style.display = 'none';
          }}
        />
        {isWinner && played && (
          <div className="absolute -bottom-1 -right-1 w-4 h-4 rounded-full bg-accent-500 flex items-center justify-center">
            <svg
              className="w-2.5 h-2.5 text-white"
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
      <div className="text-xs font-medium text-center leading-tight text-surface-600 dark:text-surface-400">
        {team.locationName}
        <br />
        <span className="text-surface-900 dark:text-white text-sm">{team.teamName}</span>
      </div>
      <div className="flex items-baseline gap-2">
        <span className="stat-number text-sm text-accent-500">
          {(team.modelOdds * 100).toFixed(1)}%
        </span>
        {played && (
          <span className="stat-number text-2xl text-surface-900 dark:text-white">
            {team.goals}
          </span>
        )}
      </div>
    </div>
  );
}

export function GameCard({ game }: GameCardProps) {
  const borderClass = predictionBorderClass(game);

  return (
    <div
      className={`glass rounded-xl px-5 py-4 ${borderClass} transition-transform duration-200 hover:scale-[1.01]`}
    >
      <div className="flex items-center justify-center gap-3">
        <TeamSide
          team={game.awayTeam}
          isWinner={game.hasBeenPlayed && game.winner === Winner.AWAY}
          played={game.hasBeenPlayed}
        />
        <div className="flex flex-col items-center gap-1 px-2">
          <div
            className={`text-[10px] font-mono font-bold tracking-widest uppercase ${
              game.hasBeenPlayed ? 'text-surface-500 dark:text-surface-400' : 'text-accent-500'
            }`}
          >
            {game.hasBeenPlayed ? 'Final' : 'VS'}
          </div>
          {!game.hasBeenPlayed && game.gameDate && (
            <div className="text-[11px] font-mono text-surface-400 dark:text-surface-500">
              {new Date(game.gameDate).toLocaleTimeString([], {
                hour: 'numeric',
                minute: '2-digit',
              })}
            </div>
          )}
          <div className="w-px h-6 bg-surface-200 dark:bg-white/[0.06]" />
        </div>
        <TeamSide
          team={game.homeTeam}
          isWinner={game.hasBeenPlayed && game.winner === Winner.HOME}
          played={game.hasBeenPlayed}
        />
      </div>
      {game.hasBeenPlayed && (
        <div className="text-center text-[11px] font-mono text-surface-400 dark:text-surface-500 mt-2 pt-2 border-t border-surface-200/50 dark:border-white/[0.04]">
          Log Loss{' '}
          <span className="stat-number text-surface-600 dark:text-surface-300">
            {game.logLoss.toFixed(4)}
          </span>
        </div>
      )}
    </div>
  );
}
