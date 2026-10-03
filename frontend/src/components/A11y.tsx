import type { ReactNode } from 'react';

/** Sets the browser tab title; React 19 hoists <title> into <head>. */
export function PageTitle({ title }: { title?: string }) {
  return <title>{title ? `${title} · NHL Odds` : 'NHL Odds'}</title>;
}

/** Announces a loading state to screen readers; skeletons themselves are hidden from them. */
export function LoadingStatus({ label = 'Loading…' }: { label?: string }) {
  return (
    <p role="status" className="sr-only">
      {label}
    </p>
  );
}

/** Horizontally scrollable wrapper (for wide tables) that keyboard users can focus and scroll. */
export function ScrollRegion({
  label,
  className = 'overflow-x-auto',
  children,
}: {
  label: string;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div role="region" aria-label={label} tabIndex={0} className={className}>
      {children}
    </div>
  );
}
