import { useEffect } from 'react';
import { useLocation, useNavigate, useNavigation } from 'react-router';
import { reportError } from '../utils/errorReporting';

// Kept the same across builds: the build that saves it is the old one, the one that reads it the new one
const KEY = 'pending-navigation';
// A reload this long after the navigation started isn't the one it caused
const MAX_AGE_MS = 30_000;

interface PendingNavigation {
  from: string;
  to: string;
  at: number;
}

const here = () => window.location.pathname + window.location.search;

/**
 * Finishes a navigation that a stale tab lost. After a deploy, a tab opened before it asks for a page's code
 * file that the new build doesn't have; React Router then reloads the page it was on, not the one clicked, and
 * reports nothing. While a navigation loads, where it's headed is kept in sessionStorage; if the page comes back
 * from a reload still where it started, the visitor is sent on to where they were going and it's reported as a
 * `chunk` failure.
 */
export function useResumeNavigation() {
  const navigation = useNavigation();
  const location = useLocation();
  const navigate = useNavigate();

  // On the first load only: a reload in the middle of a navigation leaves the entry behind
  useEffect(() => {
    let pending: PendingNavigation | null = null;
    try {
      pending = JSON.parse(sessionStorage.getItem(KEY) ?? 'null');
      sessionStorage.removeItem(KEY);
    } catch {
      return;
    }
    if (!pending || Date.now() - pending.at > MAX_AGE_MS) return;
    if (pending.from !== here() || pending.to === pending.from) return;
    reportError(
      'chunk',
      null,
      `A page's code failed to load going to ${pending.to.split('?')[0]}; resumed after a reload`,
    );
    navigate(pending.to);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- once, on load
  }, []);

  useEffect(() => {
    try {
      if (navigation.state === 'loading' && navigation.location) {
        const { pathname, search, hash } = navigation.location;
        const entry: PendingNavigation = {
          from: location.pathname + location.search,
          to: pathname + search + hash,
          at: Date.now(),
        };
        sessionStorage.setItem(KEY, JSON.stringify(entry));
      } else if (navigation.state === 'idle') {
        sessionStorage.removeItem(KEY);
      }
    } catch {
      /* storage blocked: nothing to resume */
    }
  }, [navigation.state, navigation.location, location.pathname, location.search]);
}
