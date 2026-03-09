import type { GameOddsVM } from '../types';
import { Winner } from '../types';
import { predictionBorderClass, wasCorrectlyPredicted } from '../utils/predictions';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { formatOdds } from '../utils/oddsFormat';

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
    </div>
  );
}

export function GameCard({ game }: GameCardProps) {
  const { format } = useOddsFormatContext();
  const borderClass = predictionBorderClass(game);
  const oddsColor = !game.hasBeenPlayed
    ? 'text-accent-500'
    : wasCorrectlyPredicted(game)
      ? 'text-emerald-500'
      : 'text-red-500';

  return (
    <div className={`glass rounded-xl px-5 py-4 ${borderClass}`}>
      <div className="grid grid-cols-3 items-center">
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
          {game.hasBeenPlayed ? (
            <div className="stat-number text-lg text-surface-900 dark:text-white">
              {game.awayTeam?.goals ?? 0}
              <span className="text-surface-400 dark:text-surface-500 mx-1">-</span>
              {game.homeTeam?.goals ?? 0}
            </div>
          ) : (
            game.gameDate && (
              <div className="text-[11px] font-mono text-surface-400 dark:text-surface-500">
                {new Date(game.gameDate).toLocaleTimeString([], {
                  hour: 'numeric',
                  minute: '2-digit',
                })}
              </div>
            )
          )}
        </div>
        <TeamSide
          team={game.homeTeam}
          isWinner={game.hasBeenPlayed && game.winner === Winner.HOME}
          played={game.hasBeenPlayed}
        />
        <div className="col-span-3 mt-2 pt-2 border-t border-surface-200/50 dark:border-white/[0.04]">
          <div className="grid grid-cols-3 items-center text-[11px] font-mono">
            <span className={`text-center stat-number ${oddsColor}`}>
              {game.awayTeam ? formatOdds(game.awayTeam.modelOdds, format) : '-'}
            </span>
            <span className="text-center text-surface-400 dark:text-surface-500">
              Model {game.modelName}
            </span>
            <span className={`text-center stat-number ${oddsColor}`}>
              {game.homeTeam ? formatOdds(game.homeTeam.modelOdds, format) : '-'}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}
