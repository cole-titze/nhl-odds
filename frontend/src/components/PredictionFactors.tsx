import { useEffect, useState } from 'react';
import type { GameOddsVM, PredictionFactorsVM } from '../types';
import { getPredictionFactors } from '../api/gameOdds';

// How many factors to list; the rest are summed into one line
const SHOWN = 5;

const pts = (p: number) => `${p >= 0 ? '+' : '−'}${Math.abs(p * 100).toFixed(1)} pts`;

/**
 * The main reasons behind the model's prediction (#155): how much each group of
 * features moved the home team's win probability from the average game's. Shows
 * nothing until loaded, or when the prediction has no saved factors.
 */
export function PredictionFactors({ game }: { game: GameOddsVM }) {
  const [data, setData] = useState<PredictionFactorsVM | null>(null);

  useEffect(() => {
    let current = true;
    getPredictionFactors(game.id)
      .then((d) => current && setData(d))
      .catch(() => current && setData(null));
    return () => {
      current = false;
    };
  }, [game.id]);

  if (!data || data.factors.length === 0 || !game.homeTeam || !game.awayTeam) return null;

  const home = game.homeTeam.teamName;
  const away = game.awayTeam.teamName;
  const shown = data.factors.slice(0, SHOWN);
  const rest = data.factors.slice(SHOWN).reduce((s, f) => s + f.contribution, 0);
  const predicted = data.baseHomeOdds + data.factors.reduce((s, f) => s + f.contribution, 0);
  const largest = Math.max(...shown.map((f) => Math.abs(f.contribution)), 0.0001);

  return (
    <section aria-labelledby="factors-heading" className="glass rounded-xl px-6 py-5">
      <h2
        id="factors-heading"
        className="text-xs font-mono font-bold tracking-widest uppercase text-surface-500 dark:text-surface-400 mb-1"
      >
        Why the model predicts this
      </h2>
      <p className="text-xs text-surface-500 dark:text-surface-400 mb-4">
        The average game gives the {home} a {(data.baseHomeOdds * 100).toFixed(1)}% chance at home.
        These factors move it to {(predicted * 100).toFixed(1)}%. Points are percentage points of
        win probability.
      </p>
      <ul className="space-y-3">
        {shown.map((f) => {
          const team = f.contribution >= 0 ? home : away;
          const share = (Math.abs(f.contribution) / largest) * 100;
          return (
            <li key={f.factor}>
              <div className="flex items-baseline justify-between gap-3 text-sm">
                <span className="font-semibold">{f.factor}</span>
                <span className="font-mono text-xs whitespace-nowrap">
                  {pts(Math.abs(f.contribution))} {team}
                </span>
              </div>
              {/* Bar toward the team it favors: home on the right, away on the left */}
              <svg
                aria-hidden="true"
                viewBox="0 0 100 2"
                preserveAspectRatio="none"
                className={`mt-1 h-1.5 w-full ${f.contribution >= 0 ? 'text-accent-500' : 'text-surface-400 dark:text-surface-500'}`}
              >
                <rect
                  x={f.contribution >= 0 ? 100 - share : 0}
                  width={share}
                  height="2"
                  rx="1"
                  fill="currentColor"
                />
              </svg>
              {f.detail && (
                <p className="text-xs text-surface-600 dark:text-surface-300 mt-1">{f.detail}</p>
              )}
            </li>
          );
        })}
      </ul>
      {data.factors.length > SHOWN && (
        <p className="text-xs text-surface-500 dark:text-surface-400 mt-3">
          {data.factors.length - SHOWN} smaller factors together: {pts(Math.abs(rest))}{' '}
          {rest >= 0 ? home : away}.
        </p>
      )}
    </section>
  );
}
