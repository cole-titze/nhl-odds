import { useFetch } from '../hooks/useFetch';
import { useDisplayTimeZone } from '../hooks/useDisplayTimeZone';
import { getGameLineup } from '../api/lineup';
import { Skeleton } from './Skeleton';
import { LoadingStatus } from './A11y';
import type { GameOddsVM, LineupPlayerVM, MatchupTeamVM, TeamLineupVM } from '../types';

const HEADING =
  'text-xs font-mono font-bold tracking-widest uppercase text-surface-600 dark:text-surface-400';
const SUBHEADING = 'text-xs font-semibold text-surface-600 dark:text-surface-400 mt-4 mb-1.5';

/** NHL.com's projected lines, goalies and absences for both teams (from the LineupSnapshot job) */
export function GameLineups({ game }: { game: GameOddsVM }) {
  const { data, loading, error } = useFetch(() => getGameLineup(game.id), [game.id]);
  const { locales, timeZone } = useDisplayTimeZone();

  if (loading)
    return (
      <div className="glass rounded-xl p-6 space-y-3">
        <LoadingStatus label="Loading projected lineups…" />
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-32 w-full" />
      </div>
    );

  const lineup = data?.lineup;
  // Nothing to say about a played game NHL.com never projected
  if (!lineup && game.hasBeenPlayed) return null;

  return (
    <section className="glass rounded-xl p-6" aria-labelledby="lineups-heading">
      <h2 id="lineups-heading" className={HEADING}>
        Projected Lineups
      </h2>
      {error ? (
        <p className="text-sm text-surface-600 dark:text-surface-400 mt-3">
          Couldn&apos;t load the projected lineups.
        </p>
      ) : !lineup ? (
        <p className="text-sm text-surface-600 dark:text-surface-400 mt-3">
          No projection yet. NHL.com usually posts projected lineups on the afternoon of game day.
        </p>
      ) : (
        <>
          <p className="text-xs text-surface-600 dark:text-surface-400 mt-1">
            From NHL.com&apos;s lineup projections,{' '}
            {game.hasBeenPlayed ? 'final version before puck drop, ' : ''}
            updated{' '}
            {new Date(lineup.updatedUTC).toLocaleString(locales, {
              weekday: 'short',
              hour: 'numeric',
              minute: '2-digit',
              timeZone,
            })}
            {lineup.versionCount > 1 &&
              `, revised ${lineup.versionCount === 2 ? 'once' : `${lineup.versionCount - 1} times`} since first posted`}
            .
          </p>
          <div className="grid gap-6 sm:grid-cols-2 mt-2">
            <TeamLineup team={game.awayTeam} lineup={lineup.away} />
            <TeamLineup team={game.homeTeam} lineup={lineup.home} />
          </div>
          {lineup.statusReport && (
            <div className="mt-6">
              <h3 className={SUBHEADING}>Status report</h3>
              {lineup.statusReport.split('\n').map((line, i) => (
                <p
                  key={i}
                  className="text-sm text-surface-700 dark:text-surface-300 leading-relaxed mb-2"
                >
                  {line}
                </p>
              ))}
            </div>
          )}
        </>
      )}
    </section>
  );
}

function TeamLineup({ team, lineup }: { team: MatchupTeamVM | null; lineup: TeamLineupVM }) {
  const name = team ? `${team.locationName} ${team.teamName}` : 'TBD';
  const out = [
    ...lineup.injured.map((p) => ({ ...p, why: p.note ? `injured, ${p.note}` : 'injured' })),
    ...lineup.suspended.map((p) => ({ ...p, why: 'suspended' })),
    ...lineup.scratched.map((p) => ({ ...p, why: 'scratched' })),
  ];
  const empty = lineup.forwards.length + lineup.defense.length + lineup.goalies.length === 0;

  return (
    <div>
      <h3 className="font-display font-semibold text-surface-900 dark:text-white mt-3">{name}</h3>
      {empty && (
        <p className="text-sm text-surface-600 dark:text-surface-400 mt-1">No lineup listed.</p>
      )}
      {lineup.goalies.length > 0 && (
        <>
          <h4 className={SUBHEADING}>Goalies</h4>
          <ol className="text-sm space-y-0.5">
            {lineup.goalies.map((g, i) => (
              <li key={g.name}>
                {g.name}
                {i === 0 && lineup.goalies.length > 1 && (
                  <span className="text-xs text-surface-600 dark:text-surface-400">
                    {' '}
                    · listed first
                  </span>
                )}
              </li>
            ))}
          </ol>
        </>
      )}
      <Lines title="Forwards" lines={lineup.forwards} />
      <Lines title="Defense" lines={lineup.defense} />
      {out.length > 0 && (
        <>
          <h4 className={SUBHEADING}>Out</h4>
          <ul className="text-sm space-y-0.5">
            {out.map((p) => (
              <li key={`${p.why}-${p.name}`}>
                {p.name}{' '}
                <span className="text-xs text-surface-600 dark:text-surface-400">({p.why})</span>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}

function Lines({ title, lines }: { title: string; lines: LineupPlayerVM[][] }) {
  if (lines.length === 0) return null;
  return (
    <>
      <h4 className={SUBHEADING}>{title}</h4>
      <ol className="text-sm space-y-0.5">
        {lines.map((line, i) => (
          <li key={i} className="flex gap-2">
            <span className="font-mono text-xs text-surface-600 dark:text-surface-400 w-4 shrink-0 pt-0.5">
              {i + 1}
            </span>
            <span>{line.map((p) => p.name).join(' – ')}</span>
          </li>
        ))}
      </ol>
    </>
  );
}
