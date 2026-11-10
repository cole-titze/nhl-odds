import { useEffect, useRef, useState } from 'react';
import { SITE_URL } from '../utils/site';

/** The address of a team's calendar feed (CalendarController in the web API). */
function teamCalendarUrl(teamId: number): string {
  return `${SITE_URL}/api/Calendar/Team/${teamId}.ics`;
}

/**
 * Links a team's calendar feed: webcal:// opens the device's calendar app to subscribe, and
 * the copy button is for apps that take a URL instead (Google Calendar: Other calendars → From URL).
 */
const COPIED_MS = 2500;

export function CalendarSubscribe({ teamId, teamName }: { teamId: number; teamName: string }) {
  const url = teamCalendarUrl(teamId);
  const [state, setState] = useState<'idle' | 'copied' | 'failed'>('idle');
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined);
  useEffect(() => () => clearTimeout(timer.current), []);

  const copy = async () => {
    clearTimeout(timer.current);
    try {
      await navigator.clipboard.writeText(url);
      setState('copied');
      timer.current = setTimeout(() => setState('idle'), COPIED_MS);
    } catch {
      // Stays until the next try: the link is shown so it can be copied by hand
      setState('failed');
    }
  };

  const button =
    'inline-flex items-center gap-2 px-3 py-2 rounded-lg glass text-sm font-medium transition-colors';
  const icon = 'w-4 h-4 shrink-0';
  return (
    <div className="flex flex-col items-start gap-1.5">
      <div className="flex items-center gap-2 flex-wrap">
        <a
          href={url.replace(/^https?:/, 'webcal:')}
          className={`${button} hover:text-accent-600 dark:hover:text-accent-400`}
          aria-label={`Add to calendar: the ${teamName} schedule`}
          title="Opens your calendar app and subscribes to this team's games, with the model's odds"
        >
          <svg
            aria-hidden="true"
            className={icon}
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <rect x="3" y="5" width="18" height="16" rx="2" />
            <path strokeLinecap="round" d="M3 10h18M8 3v4M16 3v4" />
          </svg>
          Add to calendar
        </a>
        <button
          type="button"
          onClick={copy}
          className={`${button} ${
            state === 'copied'
              ? 'text-emerald-700 dark:text-emerald-400'
              : 'hover:text-accent-600 dark:hover:text-accent-400'
          }`}
          aria-label={`Copy calendar link: the ${teamName} schedule`}
          title="For calendar apps that subscribe by URL, like Google Calendar (Other calendars, From URL)"
        >
          {/* Both labels share one grid cell, so the button keeps its width when the label changes */}
          <span className="inline-grid">
            <span
              className={`col-start-1 row-start-1 inline-flex items-center gap-2 ${state === 'copied' ? 'invisible' : ''}`}
            >
              <svg
                aria-hidden="true"
                className={icon}
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                strokeWidth={2}
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M10 14a4 4 0 0 0 5.66 0l3-3a4 4 0 0 0-5.66-5.66l-1 1M14 10a4 4 0 0 0-5.66 0l-3 3a4 4 0 0 0 5.66 5.66l1-1"
                />
              </svg>
              Copy calendar link
            </span>
            <span
              aria-hidden="true"
              className={`col-start-1 row-start-1 inline-flex items-center justify-center gap-2 ${
                state === 'copied' ? 'copied-pop' : 'invisible'
              }`}
            >
              <svg
                className={icon}
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                strokeWidth={2.5}
              >
                <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
              </svg>
              Link copied
            </span>
          </span>
        </button>
      </div>
      {state === 'failed' && (
        <p className="text-xs text-surface-600 dark:text-surface-400 max-w-xs break-all">
          Couldn&apos;t copy. The calendar link is{' '}
          <span className="select-all font-mono">{url}</span>
        </p>
      )}
      <p role="status" className="sr-only">
        {state === 'copied'
          ? 'Calendar link copied'
          : state === 'failed'
            ? `Couldn't copy. The calendar link is ${url}`
            : ''}
      </p>
    </div>
  );
}
