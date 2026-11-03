import { useState } from 'react';
import { SITE_URL } from '../utils/site';

/** The address of a team's calendar feed (CalendarController in the web API). */
function teamCalendarUrl(teamId: number): string {
  return `${SITE_URL}/api/Calendar/Team/${teamId}.ics`;
}

/**
 * Links a team's calendar feed: webcal:// opens the device's calendar app to subscribe, and
 * the copy button is for apps that take a URL instead (Google Calendar: Other calendars → From URL).
 */
export function CalendarSubscribe({ teamId, teamName }: { teamId: number; teamName: string }) {
  const url = teamCalendarUrl(teamId);
  const [status, setStatus] = useState('');

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setStatus('Calendar link copied');
    } catch {
      setStatus(`Couldn't copy. The link is ${url}`);
    }
    setTimeout(() => setStatus(''), 4000);
  };

  const button =
    'inline-flex items-center gap-2 px-3 py-2 rounded-lg glass text-sm font-medium hover:text-accent-600 dark:hover:text-accent-400 transition-colors';
  return (
    <div className="flex items-center gap-2 flex-wrap">
      <a
        href={url.replace(/^https?:/, 'webcal:')}
        className={button}
        aria-label={`Subscribe in calendar: ${teamName} schedule`}
      >
        <svg
          aria-hidden="true"
          className="w-4 h-4"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={2}
        >
          <rect x="3" y="5" width="18" height="16" rx="2" />
          <path strokeLinecap="round" d="M3 10h18M8 3v4M16 3v4" />
        </svg>
        Subscribe in calendar
      </a>
      <button
        type="button"
        onClick={copy}
        className={button}
        aria-label={`Copy link to the ${teamName} calendar feed`}
      >
        Copy link
      </button>
      <p role="status" className="sr-only">
        {status}
      </p>
    </div>
  );
}
