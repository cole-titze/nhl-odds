import { Skeleton } from './Skeleton';

/**
 * A small spinning ring, sized by `className` (it inherits the text color). Decorative: say what's loading with
 * `LoadingStatus` or a status line. With reduced motion it holds still (`.spinner` in index.css).
 */
export function Spinner({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg className={`spinner ${className}`} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="3" opacity="0.25" />
      <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
    </svg>
  );
}

const ROW_WIDTHS = ['w-40', 'w-52', 'w-36'];

/** Stands in for the search page's results: a section of team-like rows and one of game-like rows. */
export function SearchResultsSkeleton() {
  return (
    <div className="space-y-5" aria-hidden="true" data-loading="search">
      {[3, 2].map((rows, s) => (
        <div key={s} className="glass rounded-xl p-5">
          <Skeleton className="h-5 w-24 mb-4" />
          <div className="space-y-3">
            {Array.from({ length: rows }).map((_, i) => (
              <div key={i} className="flex items-center gap-3">
                {s === 0 && <Skeleton className="h-8 w-8 shrink-0 !rounded-full" />}
                <Skeleton className={`h-4 ${ROW_WIDTHS[i % ROW_WIDTHS.length]}`} />
                <Skeleton className="h-3 w-12" />
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
