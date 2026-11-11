import { Suspense, useRef, type ReactNode } from 'react';
import { useHydrated } from '../hooks/useHydrated';
import { useNearViewport } from '../hooks/useNearViewport';

/**
 * Holds a chart's space with an empty box until it scrolls near the screen, then renders it. Pair it
 * with a React.lazy chart so the chart library downloads and draws only then, not while the page
 * first loads. Server HTML always gets the empty box: Recharts sizes itself with inline styles,
 * which the CSP blocks in server HTML.
 * @param boxClassName sizes the empty box like the chart, e.g. "h-[300px]" (a class, not an inline
 * style, for the same CSP reason)
 */
export function LazyChart({
  boxClassName,
  children,
}: {
  boxClassName: string;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const hydrated = useHydrated();
  const near = useNearViewport(ref);

  // data-lazy-chart: the a11y scan scrolls pages that have one so their charts get checked
  if (!hydrated || !near) return <div ref={ref} className={boxClassName} data-lazy-chart="" />;
  return <Suspense fallback={<div className={boxClassName} />}>{children}</Suspense>;
}
