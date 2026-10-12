import { useState, useEffect, useRef } from 'react';

interface FetchState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

const sameDeps = (a: unknown[], b: unknown[]) =>
  a.length === b.length && a.every((value, i) => Object.is(value, b[i]));

/**
 * `initial` is data the server already loaded for the first deps (a route
 * loader's result), so the first fetch is skipped. Later deps always fetch.
 */
export function useFetch<T>(
  fetcher: () => Promise<T>,
  deps: unknown[],
  initial?: T | null,
): FetchState<T> {
  const [state, setState] = useState<FetchState<T>>(
    initial != null
      ? { data: initial, loading: false, error: null }
      : { data: null, loading: true, error: null },
  );
  const [stateDeps, setStateDeps] = useState(deps);
  const skipFetch = useRef(initial != null);

  // When the deps change, go back to loading during render rather than in the effect,
  // so there's no render with the previous data and no extra cascading render
  // (https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes)
  if (!sameDeps(stateDeps, deps)) {
    setStateDeps(deps);
    setState({ data: null, loading: true, error: null });
  }

  useEffect(() => {
    if (skipFetch.current) {
      skipFetch.current = false;
      return;
    }
    let cancelled = false;

    fetcher()
      .then((data) => {
        if (!cancelled) setState({ data, loading: false, error: null });
      })
      .catch((err) => {
        if (!cancelled) setState({ data: null, loading: false, error: err.message });
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return state;
}
