import type { ReactNode } from 'react';
import { useLocation } from 'react-router';
import { SITE_URL } from '../utils/site';

const DEFAULT_DESCRIPTION =
  'Machine learning win probabilities for every NHL game, compared with sportsbook and Kalshi odds.';

/**
 * Sets the browser tab title and the tags search engines and link previews
 * read; React 19 hoists these into <head>. noindex keeps a page out of search.
 */
export function PageTitle({
  title,
  description = DEFAULT_DESCRIPTION,
  noindex = false,
}: {
  title?: string;
  description?: string;
  noindex?: boolean;
}) {
  const { pathname } = useLocation();
  const fullTitle = title ? `${title} · NHL Odds` : 'NHL Odds';
  // Without the query string, so URL variants count as one page
  const url = SITE_URL + pathname;
  return (
    <>
      <title>{fullTitle}</title>
      <meta name="description" content={description} />
      {noindex ? <meta name="robots" content="noindex" /> : <link rel="canonical" href={url} />}
      <meta property="og:type" content="website" />
      <meta property="og:site_name" content="NHL Odds" />
      <meta property="og:title" content={fullTitle} />
      <meta property="og:description" content={description} />
      <meta property="og:url" content={url} />
      <meta name="twitter:card" content="summary" />
    </>
  );
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
