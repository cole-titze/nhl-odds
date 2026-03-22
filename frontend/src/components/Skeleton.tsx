export function Skeleton({ className = '' }: { className?: string }) {
  return (
    <div
      className={`rounded-lg bg-surface-200/60 dark:bg-white/[0.06] overflow-hidden relative ${className}`}
    >
      <div className="absolute inset-0 skeleton-shimmer" />
    </div>
  );
}

export function CardSkeleton() {
  return (
    <div className="glass rounded-xl p-5 space-y-4">
      <div className="flex items-center justify-center gap-6">
        <div className="flex flex-col items-center gap-2 flex-1">
          <Skeleton className="h-14 w-14 !rounded-full" />
          <Skeleton className="h-3 w-16" />
          <Skeleton className="h-4 w-12" />
        </div>
        <Skeleton className="h-4 w-6" />
        <div className="flex flex-col items-center gap-2 flex-1">
          <Skeleton className="h-14 w-14 !rounded-full" />
          <Skeleton className="h-3 w-16" />
          <Skeleton className="h-4 w-12" />
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
