// Sends errors from visitors' browsers to /api/ClientError, which saves them to
// ErrorLog (Source "Frontend") for the Admin page. Best effort: a report that
// fails is dropped, and reporting can never throw into the page.

import { BUILD_VERSION } from '../build';

/** error: window.onerror; unhandledrejection; render: the root error boundary; api: a failed
 * API call (client.ts); chunk: a file of the page's code that failed to load (a stale tab) */
export type ClientErrorKind = 'error' | 'unhandledrejection' | 'render' | 'api' | 'chunk';

interface ClientErrorReport {
  kind: ClientErrorKind;
  name: string | null;
  message: string;
  stack: string | null;
  page: string;
  version: string;
}

// The API also limits each visitor (a burst of 5, then 1 a minute); this keeps a
// page stuck in an error loop from even trying
const MAX_REPORTS_PER_PAGE_LOAD = 5;
const MAX_STACK_CHARS = 4000;

const sent = new Set<string>();
let reportCount = 0;

// Errors thrown by browser extensions or blocked third-party scripts: nothing
// this site can fix
const EXTENSION_URL = /(chrome|moz|safari(-web)?)-extension:\/\//;

function toReport(kind: ClientErrorKind, error: unknown, fallbackMessage?: string) {
  const err = error instanceof Error ? error : null;
  const message = (
    err?.message ||
    (typeof error === 'string' ? error : '') ||
    fallbackMessage ||
    ''
  ).trim();
  const stack = err?.stack?.slice(0, MAX_STACK_CHARS) ?? null;
  return {
    kind,
    name: err?.name ?? null,
    message,
    stack,
    page: window.location.pathname,
    version: BUILD_VERSION,
  } satisfies ClientErrorReport;
}

/** Whether a report is noise rather than a bug in the site. Exported for tests. */
export function isIgnored(report: Pick<ClientErrorReport, 'name' | 'message' | 'stack'>) {
  // Cross-origin script errors are reduced to this, with no detail to act on
  if (!report.message || report.message === 'Script error.') return true;
  // A cancelled fetch (navigating away mid-request) isn't a failure
  if (report.name === 'AbortError') return true;
  // Chrome's benign "ResizeObserver loop completed with undelivered notifications"
  if (report.message.startsWith('ResizeObserver loop')) return true;
  return EXTENSION_URL.test(report.stack ?? '');
}

export function reportError(kind: ClientErrorKind, error: unknown, fallbackMessage?: string) {
  // Only production builds report, so dev server errors stay in the console
  if (typeof window === 'undefined' || !import.meta.env.PROD) return;
  try {
    const report = toReport(kind, error, fallbackMessage);
    if (isIgnored(report)) return;
    const key = `${report.kind}|${report.name}|${report.message}`;
    if (sent.has(key) || reportCount >= MAX_REPORTS_PER_PAGE_LOAD) return;
    sent.add(key);
    reportCount++;
    // keepalive lets the report finish if the error makes the visitor leave
    fetch('/api/ClientError', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(report),
      keepalive: true,
    }).catch(() => {});
  } catch {
    /* never let reporting break the page */
  }
}

/** Reports uncaught errors and unhandled promise rejections. Call once, before hydrating. */
export function installErrorReporting() {
  window.addEventListener('error', (event) => {
    // Failed <img>/<script> loads don't bubble here (that needs capture), so
    // this is a thrown exception; event.error is missing for cross-origin scripts
    reportError('error', event.error, event.message);
  });
  window.addEventListener('unhandledrejection', (event) => {
    reportError('unhandledrejection', event.reason);
  });
  // Vite's preload of a code file failed: usually a tab opened before a deploy asking for a file
  // the new build doesn't have. React Router reloads the page itself when a page's own module
  // fails; for anything else, reload once (per build, so a broken file can't loop) to get the
  // current build
  window.addEventListener('vite:preloadError', (event) => {
    reportError(
      'chunk',
      (event as Event & { payload?: unknown }).payload,
      'A code file failed to load',
    );
    const key = `reloaded-for-${BUILD_VERSION}`;
    try {
      if (sessionStorage.getItem(key)) return;
      sessionStorage.setItem(key, '1');
    } catch {
      return;
    }
    event.preventDefault();
    window.location.reload();
  });
}

/**
 * Reports an API call that failed for a reason worth knowing about: a 5xx answer, or the
 * request not getting through while the browser is online. 4xx answers are the API working
 * (a game that doesn't exist, an admin key missing), so they aren't reported.
 */
export function reportApiFailure(
  method: string,
  path: string,
  status: number | null,
  error?: unknown,
) {
  if (typeof window === 'undefined') return;
  if (status == null) {
    if (!navigator.onLine) return;
    if (error instanceof Error && error.name === 'AbortError') return;
  } else if (status < 500) {
    return;
  }
  const route = path.split('?')[0];
  // The message is the same for every failure of an endpoint, so the API keeps one per window
  const message =
    status == null
      ? `${method} ${route} didn't get a response`
      : `${method} ${route} answered ${status}`;
  const failure = new Error(message);
  failure.name = 'ApiError';
  failure.stack = `ApiError: ${message}\n    request: ${path}${
    error instanceof Error ? `\n    cause: ${error.name}: ${error.message}` : ''
  }`;
  reportError('api', failure);
}
