export function Skeleton({ className = '' }: { className?: string }) {
  return <div className={`animate-pulse rounded bg-gray-200 dark:bg-gray-700 ${className}`} />;
}

export function CardSkeleton() {
  return (
    <div className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 space-y-3">
      <Skeleton className="h-5 w-1/3" />
      <Skeleton className="h-8 w-full" />
      <Skeleton className="h-5 w-1/2" />
    </div>
  );
}
