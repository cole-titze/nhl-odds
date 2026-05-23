import type { ReactNode } from 'react';
import type { StrategyResult } from '../utils/bettingStrategies';
import { getTextClass } from '../utils/colorClass';

const STRATEGY_COLORS: Record<string, string> = {
  'In-House Winner': '#3b82f6',
  'Value Bets': '#22c55e',
  'In-House Underdog': '#f97316',
  Confidence: '#a855f7',
  'Spread Bet': '#3b82f6',
  'Spread Value': '#22c55e',
  'O/U Bet': '#3b82f6',
  'O/U Value': '#22c55e',
};

interface Props {
  result: StrategyResult;
  children?: ReactNode;
}

export function StrategyCard({ result: r, children }: Props) {
  const color = STRATEGY_COLORS[r.name] ?? '#737373';
  const plPositive = r.totalPL >= 0;
  return (
    <div className="glass rounded-xl p-5">
      <div className="flex items-center justify-between mb-3">
        <div className={`text-xs font-semibold uppercase tracking-wider ${getTextClass(color)}`}>
          {r.name}
        </div>
        {children && <div className="flex flex-wrap gap-1">{children}</div>}
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="text-center">
          <div className="stat-number text-2xl text-surface-900 dark:text-white">{r.totalBets}</div>
          <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
            Bets
          </div>
        </div>
        <div className="text-center">
          <div className="stat-number text-2xl text-surface-900 dark:text-white">
            {(r.winRate * 100).toFixed(1)}%
          </div>
          <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
            Win Rate
          </div>
        </div>
        <div className="text-center">
          <div
            className={`stat-number text-2xl ${plPositive ? 'text-emerald-500' : 'text-red-500'}`}
          >
            {plPositive ? '+' : ''}
            {r.totalPL.toFixed(2)}u
          </div>
          <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
            P/L
          </div>
        </div>
        <div className="text-center">
          <div
            className={`stat-number text-2xl ${r.roi >= 0 ? 'text-emerald-500' : 'text-red-500'}`}
          >
            {r.roi >= 0 ? '+' : ''}
            {r.roi.toFixed(1)}%
          </div>
          <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
            ROI
          </div>
        </div>
      </div>
    </div>
  );
}
