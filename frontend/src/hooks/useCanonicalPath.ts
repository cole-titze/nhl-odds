import { useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router';

/**
 * After client-side navigation to a page by another path (search links have only the id), swaps the
 * address bar to the page's own path, as the server's redirect does on a full load. Keeps the query
 * string, hash, state and scroll position.
 */
export function useCanonicalPath(path: string | null | undefined) {
  const location = useLocation();
  const navigate = useNavigate();
  useEffect(() => {
    if (path && location.pathname !== path)
      navigate(
        { pathname: path, search: location.search, hash: location.hash },
        { replace: true, state: location.state, preventScrollReset: true },
      );
  }, [path, location, navigate]);
}
