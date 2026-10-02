export function Skeleton({ className = '' }: { className?: string }) {
  return (
    <div
      className={`rounded-lg bg-surface-200/60 dark:bg-white/[0.06] overflow-hidden relative ${className}`}
    >
      <div className="absolute inset-0 skeleton-shimmer" />
    </div>
  );
}

// Bars sit inline inside text-sized containers, so each line keeps the same
// line-height as the GameCard text it stands in for.
const textBar = 'inline-block align-middle h-[0.8em]';

function TeamSideSkeleton() {
  return (
    <div className="flex-1 flex flex-col items-center gap-2">
      <Skeleton className="h-20 w-20 !rounded-full" />
      <div className="text-xs font-medium text-center leading-tight">
        <Skeleton className={`${textBar} w-14`} />
        <br />
        <span className="text-sm">
          <Skeleton className={`${textBar} w-20`} />
        </span>
      </div>
    </div>
  );
}

function OddsRowSkeleton({ className = '' }: { className?: string }) {
  return (
    <div className={`grid grid-cols-3 items-center text-[11px] font-mono ${className}`}>
      {['w-10', 'w-16', 'w-10'].map((w, i) => (
        <span key={i} className="text-center">
          <Skeleton className={`${textBar} ${w}`} />
        </span>
      ))}
    </div>
  );
}

// Mirrors GameCard's layout (regular-season game with model + two bookmaker rows)
export function CardSkeleton() {
  return (
    <div className="glass rounded-xl px-5 py-4">
      <div className="grid grid-cols-3 items-center">
        <TeamSideSkeleton />
        <div className="flex flex-col items-center gap-1 px-2">
          <div className="text-[10px] font-mono">
            <Skeleton className={`${textBar} w-5`} />
          </div>
          <div className="text-[11px] font-mono">
            <Skeleton className={`${textBar} w-12`} />
          </div>
        </div>
        <TeamSideSkeleton />
        <div className="col-span-3 mt-2 pt-2 border-t border-surface-200/50 dark:border-white/[0.04]">
          <OddsRowSkeleton />
          <OddsRowSkeleton className="mt-1" />
          <OddsRowSkeleton className="mt-1" />
        </div>
      </div>
    </div>
  );
}

export function JobCardSkeleton() {
  return (
    <div className="glass rounded-xl p-6 space-y-4">
      <div className="flex items-center justify-between">
        <Skeleton className="h-5 w-32" />
        <Skeleton className="h-6 w-20 !rounded-lg" />
      </div>
      <div className="space-y-2">
        <div className="flex justify-between">
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-4 w-36" />
        </div>
        <div className="flex justify-between">
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-4 w-36" />
        </div>
      </div>
      <Skeleton className="h-10 w-full !rounded-lg" />
    </div>
  );
}

export function HealthCheckTableSkeleton() {
  return (
    <div className="glass rounded-xl overflow-hidden">
      <div className="px-4 py-3 border-b border-surface-200 dark:border-white/[0.06]">
        <div className="flex gap-4">
          {Array.from({ length: 9 }).map((_, i) => (
            <Skeleton key={i} className={`h-3 ${i === 0 ? 'w-16' : 'w-10'}`} />
          ))}
        </div>
      </div>
      {Array.from({ length: 4 }).map((_, i) => (
        <div
          key={i}
          className="px-4 py-3 border-b border-surface-200 dark:border-white/[0.04] last:border-0"
        >
          <div className="flex gap-4">
            {Array.from({ length: 9 }).map((_, j) => (
              <Skeleton key={j} className={`h-4 ${j === 0 ? 'w-16' : 'w-10'}`} />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

export function StatCardSkeleton() {
  return (
    <div className="glass rounded-xl p-5 text-center">
      <Skeleton className="h-9 w-20 mx-auto" />
      <Skeleton className="h-3 w-24 mx-auto mt-2" />
    </div>
  );
}
