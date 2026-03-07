export function Skeleton({ className = '' }: { className?: string }) {
  return (
    <div
      className={`animate-pulse rounded-lg bg-surface-200/60 dark:bg-white/[0.04] ${className}`}
    />
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
