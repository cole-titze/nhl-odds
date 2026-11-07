export function StatCard({
  label,
  value,
  sub,
  accent,
}: {
  label: string;
  value: string;
  sub?: string;
  accent?: boolean;
}) {
  return (
    <div className="glass rounded-xl p-4 text-center">
      <div
        className={`stat-number text-2xl font-bold ${accent ? 'text-accent-600 dark:text-accent-400' : ''}`}
      >
        {value}
      </div>
      <div className="text-xs text-surface-500 dark:text-surface-400 mt-1">{label}</div>
      {sub && <div className="text-xs text-surface-500 dark:text-surface-400 mt-0.5">{sub}</div>}
    </div>
  );
}
