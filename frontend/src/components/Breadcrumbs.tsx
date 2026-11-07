import { Link } from 'react-router';

export type Crumb = { name: string; path: string };

/**
 * Visible breadcrumbs (#169). Pages pass the same list to PageTitle's schema
 * as breadcrumbs(crumbs), so the BreadcrumbList markup matches what's shown.
 * The last crumb is the page itself.
 */
export function Breadcrumbs({ items }: { items: Crumb[] }) {
  return (
    <nav aria-label="Breadcrumb" className="mb-4 text-sm">
      <ol className="flex flex-wrap items-center gap-x-1.5 text-surface-500 dark:text-surface-400">
        {items.map((crumb, i) => {
          const last = i === items.length - 1;
          return (
            <li key={crumb.path} className="flex items-center gap-x-1.5 min-w-0">
              {i > 0 && <span aria-hidden="true">›</span>}
              {last ? (
                <span
                  aria-current="page"
                  className="inline-block py-0.5 font-medium text-surface-700 dark:text-surface-200"
                >
                  {crumb.name}
                </span>
              ) : (
                <Link
                  to={crumb.path}
                  className="inline-block py-0.5 underline-offset-2 hover:underline hover:text-accent-600 dark:hover:text-accent-400"
                >
                  {crumb.name}
                </Link>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
