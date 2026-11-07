import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react';
import { Form, useLocation, useNavigate } from 'react-router';
import {
  MAX_QUERY_CHARS,
  MIN_QUERY_CHARS,
  suggest,
  type GameResult,
  type SuggestResults,
} from '../api/search';
import { TeamLogo } from './TeamLogo';

// Typing waits this long before asking for suggestions, so each keystroke isn't a request
const SUGGEST_DELAY_MS = 200;

export function SearchIcon({ className }: { className: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      aria-hidden="true"
      viewBox="0 0 20 20"
      fill="currentColor"
    >
      <path
        fillRule="evenodd"
        d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.45 4.39l3.08 3.08a.75.75 0 11-1.06 1.06l-3.08-3.08A7 7 0 012 9z"
        clipRule="evenodd"
      />
    </svg>
  );
}

interface Option {
  key: string;
  group: 'Teams' | 'Players' | 'Games' | 'all';
  to: string;
  label: string;
  detail?: string;
  logo?: { src: string; alt: string };
}

// Said after each option, since the visible group headings are hidden from screen readers
const GROUP_NAMES = { Teams: 'team', Players: 'player', Games: 'game' } as const;

// Game dates are calendar dates (Eastern), so they're formatted without a time zone shift
function shortDate(isoDate: string) {
  return new Date(`${isoDate.slice(0, 10)}T12:00:00Z`).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    timeZone: 'UTC',
  });
}

function gameDetail(g: GameResult) {
  const date = shortDate(g.gameDate);
  return g.played && g.away.goals != null && g.home.goals != null
    ? `${date} · ${g.away.goals}–${g.home.goals}`
    : date;
}

/** The suggestion list's rows, in the order arrow keys move through them; the last one is the full search. */
function optionsFor(results: SuggestResults, text: string): Option[] {
  const options: Option[] = [
    ...results.teams.map((t) => ({
      key: `t${t.teamId}`,
      group: 'Teams' as const,
      to: `/team/${t.teamId}`,
      label: t.name,
      detail: t.active ? undefined : 'Former team',
      logo: { src: t.logoUri, alt: '' },
    })),
    ...results.players.map((p) => ({
      key: `p${p.playerId}`,
      group: 'Players' as const,
      // Players have no page of their own yet: their team's page, else a full search for their name
      to: p.teamId != null ? `/team/${p.teamId}` : `/search?${new URLSearchParams({ q: p.name })}`,
      label: p.name,
      detail: [p.position, p.teamAbbreviation].filter(Boolean).join(' · ') || undefined,
    })),
    ...results.games.map((g) => ({
      key: `g${g.gameId}`,
      group: 'Games' as const,
      to: `/game/${g.gameId}`,
      label: `${g.away.abbreviation} @ ${g.home.abbreviation}`,
      detail: gameDetail(g),
    })),
  ];
  options.push({
    key: 'all',
    group: 'all',
    to: `/search?${new URLSearchParams({ q: text.trim() })}`,
    label: `See all results for “${text.trim()}”`,
  });
  return options;
}

/**
 * The navbar's search box. A plain GET form to /search, so it works before the page's script loads; with
 * script, it suggests teams, players and games as you type (the ARIA combobox pattern: arrow keys move through
 * the list, Enter opens the highlighted one, Escape closes it). Enter with nothing highlighted is the full search.
 */
export function SearchBox() {
  const id = useId();
  const listId = `${id}-list`;
  const navigate = useNavigate();
  const { pathname, search } = useLocation();
  const [text, setText] = useState('');
  const [results, setResults] = useState<SuggestResults | null>(null);
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined);
  const request = useRef<AbortController>(undefined);

  const options = results && results.query === text.trim() ? optionsFor(results, text) : [];
  // Only the "see all" row means nothing matched: no list, just the form
  const showList = open && options.length > 1;

  function close() {
    setOpen(false);
    setActive(-1);
  }

  // Going somewhere (a suggestion, a search, any link) closes the list and empties the box. Adjusted during
  // render, as React recommends, rather than in an effect that renders twice; the timer and request are
  // stopped where the navigation starts (go) or ignored when they land (the query no longer matches)
  const location = pathname + search;
  const [shownLocation, setShownLocation] = useState(location);
  if (location !== shownLocation) {
    setShownLocation(location);
    setText('');
    setResults(null);
    setOpen(false);
    setActive(-1);
  }

  useEffect(
    () => () => {
      clearTimeout(timer.current);
      request.current?.abort();
    },
    [],
  );

  function onChange(value: string) {
    setText(value);
    setActive(-1);
    setOpen(true);
    clearTimeout(timer.current);
    request.current?.abort();
    if (value.trim().length < MIN_QUERY_CHARS) {
      setResults(null);
      return;
    }
    timer.current = setTimeout(async () => {
      const controller = new AbortController();
      request.current = controller;
      const found = await suggest(value, controller.signal);
      if (!controller.signal.aborted) setResults(found);
    }, SUGGEST_DELAY_MS);
  }

  function go(option: Option) {
    clearTimeout(timer.current);
    request.current?.abort();
    close();
    navigate(option.to);
  }

  function onKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      if (options.length <= 1) return;
      e.preventDefault();
      setOpen(true);
      const step = e.key === 'ArrowDown' ? 1 : -1;
      // From the box, down goes to the first row and up to the last
      setActive((i) =>
        i < 0 ? (step > 0 ? 0 : options.length - 1) : (i + step + options.length) % options.length,
      );
    } else if (e.key === 'Enter' && showList && active >= 0) {
      e.preventDefault();
      go(options[active]);
    } else if (e.key === 'Escape') {
      if (showList) {
        e.preventDefault();
        close();
      } else if (text) {
        // A second Escape clears the box, as in the ARIA combobox pattern
        e.preventDefault();
        onChange('');
      }
    }
  }

  const activeId = showList && active >= 0 ? `${id}-opt-${active}` : undefined;
  const count = options.length - 1;

  return (
    <Form
      role="search"
      method="get"
      action="/search"
      className="hidden md:block mr-1"
      onBlur={(e) => {
        // Focus moving out of the box and its list closes the list
        if (!e.currentTarget.contains(e.relatedTarget as Node | null)) close();
      }}
    >
      <label htmlFor={`${id}-input`} className="sr-only">
        Search teams, players, games and articles
      </label>
      <div className="relative">
        <SearchIcon className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-surface-500 dark:text-surface-400" />
        <input
          id={`${id}-input`}
          type="search"
          name="q"
          required
          minLength={MIN_QUERY_CHARS}
          maxLength={MAX_QUERY_CHARS}
          autoComplete="off"
          enterKeyHint="search"
          placeholder="Search"
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={showList}
          aria-controls={listId}
          aria-activedescendant={activeId}
          value={text}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={onKeyDown}
          onFocus={() => setOpen(true)}
          className="w-40 lg:w-52 rounded-lg border border-surface-300 dark:border-white/15 bg-white/70 dark:bg-white/[0.04] py-1.5 pl-8 pr-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-accent-500"
        />
        {/* Tells screen readers there's a list to arrow into, after each new set of suggestions */}
        <p role="status" className="sr-only">
          {showList
            ? `${count} suggestion${count === 1 ? '' : 's'}. Use the up and down arrows to choose.`
            : ''}
        </p>
        <ul
          id={listId}
          role="listbox"
          aria-label="Suggestions"
          hidden={!showList}
          className="absolute right-0 top-full mt-2 w-80 max-w-[calc(100vw-2rem)] overflow-hidden rounded-xl border border-surface-200 dark:border-white/10 bg-white dark:bg-surface-900 py-1.5 shadow-xl shadow-black/10 dark:shadow-black/40"
        >
          {options.map((o, i) => {
            const firstOfGroup = o.group !== 'all' && (i === 0 || options[i - 1].group !== o.group);
            return (
              <li
                key={o.key}
                id={`${id}-opt-${i}`}
                role="option"
                aria-selected={i === active}
                // Keeps focus in the box, so the click lands before blur closes the list
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => go(o)}
                onMouseMove={() => setActive(i)}
                className={`${o.group === 'all' ? 'mt-1 border-t border-surface-200 dark:border-white/10 pt-1' : ''}`}
              >
                {firstOfGroup && (
                  // Visual only: each option says its group itself (GROUP_NAMES), since a heading inside an option would be read as part of it
                  <div
                    aria-hidden="true"
                    className="px-3 pb-1 pt-2 text-[11px] font-semibold uppercase tracking-wider text-surface-600 dark:text-surface-400"
                  >
                    {o.group}
                  </div>
                )}
                <div
                  className={`mx-1.5 flex cursor-pointer items-center gap-2.5 rounded-lg px-2 py-1.5 text-sm ${
                    i === active
                      ? 'bg-surface-100 text-surface-900 dark:bg-white/[0.08] dark:text-white'
                      : 'text-surface-800 dark:text-surface-200'
                  }`}
                >
                  {o.logo && (
                    <TeamLogo src={o.logo.src} alt={o.logo.alt} className="h-5 w-5 shrink-0" />
                  )}
                  <span
                    className={`min-w-0 flex-1 truncate ${o.group === 'all' ? 'font-medium' : ''}`}
                  >
                    {o.label}
                    {o.group !== 'all' && <span className="sr-only">, {GROUP_NAMES[o.group]}</span>}
                  </span>
                  {o.detail && (
                    <span className="shrink-0 text-xs text-surface-600 dark:text-surface-400">
                      {o.detail}
                    </span>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      </div>
    </Form>
  );
}
