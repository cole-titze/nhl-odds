import type { ReactNode } from 'react';
import { useLocation } from 'react-router';
import { SITE_URL } from '../utils/site';
import { jsonLdString, pageGraph, type JsonLd } from '../utils/structuredData';

const DEFAULT_DESCRIPTION =
  'Machine learning win probabilities for every NHL game, compared with sportsbook and Kalshi odds.';

/**
 * Sets the browser tab title and the tags search engines and link previews
 * read; React 19 hoists these into <head>. noindex keeps a page out of search.
 * Indexable pages also get schema.org JSON-LD: the site, a WebPage for this
 * page, and any items in `schema` (utils/structuredData.ts builds them).
 */
export function PageTitle({
  title,
  description = DEFAULT_DESCRIPTION,
  noindex = false,
  schema = [],
}: {
  title?: string;
  description?: string;
  noindex?: boolean;
  schema?: (JsonLd | null)[];
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
      {!noindex && (
        <script
          type="application/ld+json"
          // Data, not script: React never runs it and the CSP doesn't apply. jsonLdString escapes `<`.
          dangerouslySetInnerHTML={{
            __html: jsonLdString(
              pageGraph(
                { url, name: fullTitle, description },
                schema.filter((s): s is JsonLd => s != null),
              ),
            ),
          }}
        />
      )}
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
