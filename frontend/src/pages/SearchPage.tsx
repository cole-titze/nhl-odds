import { useEffect, useRef, useState, type ReactNode } from 'react';
import {
  Form,
  Link,
  useLoaderData,
  useNavigation,
  useSearchParams,
  type ClientLoaderFunctionArgs,
  type LoaderFunctionArgs,
} from 'react-router';
import {
  MATCH_END,
  MATCH_START,
  MAX_QUERY_CHARS,
  MIN_QUERY_CHARS,
  search,
  type ArticleResult,
  type GameResult,
  type PlayerResult,
  type SearchOutcome,
  type TeamResult,
} from '../api/search';
import { forwardClientIp } from '../api/client';
import { TeamLogo } from '../components/TeamLogo';
import { PageTitle } from '../components/A11y';
import { SearchResultsSkeleton, Spinner } from '../components/Loading';
import { SearchIcon } from '../components/SearchBox';
import { useDelayedFlag } from '../hooks/useDelayedFlag';
import { playerPath } from '../utils/players';

// Typing waits this long before searching, so each keystroke isn't a request
const TYPING_DELAY_MS = 300;

const queryOf = (request: Request) => new URL(request.url).searchParams.get('q') ?? '';

// The first load renders on the server; after that the browser asks the API itself
export async function loader({ request }: LoaderFunctionArgs) {
  const q = queryOf(request);
  return { q, outcome: await search(q, forwardClientIp(request)) };
}

export async function clientLoader({ request }: ClientLoaderFunctionArgs) {
  const q = queryOf(request);
  return { q, outcome: await search(q) };
}

const sectionClass = 'glass rounded-xl p-5';
const headingClass = 'font-display text-lg font-bold tracking-tight mb-3';
const linkRowClass =
  'flex items-center gap-3 rounded-lg px-2 py-2 -mx-2 hover:bg-surface-100/60 dark:hover:bg-white/[0.04] transition-colors';
const mutedClass = 'text-surface-600 dark:text-surface-400';

/** Turns the API's match markers into <mark>, keeping the snippet plain text (no HTML from the API). */
function Highlighted({ text }: { text: string }) {
  const parts: ReactNode[] = [];
  let rest = text;
  let key = 0;
  while (rest.length > 0) {
    const start = rest.indexOf(MATCH_START);
    if (start < 0) {
      parts.push(rest);
      break;
    }
    const end = rest.indexOf(MATCH_END, start);
    parts.push(rest.slice(0, start));
    const matched = rest.slice(start + 1, end < 0 ? undefined : end);
    parts.push(
      <mark
        key={key++}
        className="rounded-sm bg-accent-500/20 px-0.5 text-inherit dark:bg-accent-400/25"
      >
        {matched}
      </mark>,
    );
    rest = end < 0 ? '' : rest.slice(end + 1);
  }
  return <>{parts}</>;
}

// Game dates are calendar dates (Eastern), so they're formatted without a time zone shift
function formatDate(isoDate: string) {
  return new Date(`${isoDate.slice(0, 10)}T12:00:00Z`).toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: 'UTC',
  });
}

function TeamResults({ teams }: { teams: TeamResult[] }) {
  return (
    <section aria-labelledby="results-teams" className={sectionClass}>
      <h2 id="results-teams" className={headingClass}>
        Teams
      </h2>
      <ul>
        {teams.map((t) => (
          <li key={t.teamId}>
            <Link to={`/team/${t.teamId}`} className={linkRowClass}>
              <TeamLogo src={t.logoUri} alt="" className="h-8 w-8 shrink-0" />
              <span className="font-medium">{t.name}</span>
              <span className={`text-xs font-mono ${mutedClass}`}>{t.abbreviation}</span>
              {!t.active && <span className={`text-xs ${mutedClass}`}>(former team)</span>}
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}

function PlayerResults({ players }: { players: PlayerResult[] }) {
  return (
    <section aria-labelledby="results-players" className={sectionClass}>
      <h2 id="results-players" className={headingClass}>
        Players
      </h2>
      <ul>
        {players.map((p) => {
          const details = [p.position, p.teamName, p.isActive ? null : 'inactive']
            .filter(Boolean)
            .join(' · ');
          const content = (
            <>
              <span className="font-medium">{p.name}</span>
              {details && <span className={`text-sm ${mutedClass}`}>{details}</span>}
            </>
          );
          return (
            <li key={p.playerId}>
              <Link to={playerPath(p.playerId)} className={linkRowClass}>
                {content}
              </Link>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function GameResults({ games }: { games: GameResult[] }) {
  return (
    <section aria-labelledby="results-games" className={sectionClass}>
      <h2 id="results-games" className={headingClass}>
        Games
      </h2>
      <ul>
        {/* The API ranks games nearest today first; read in date order, they're easier to scan */}
        {[...games]
          .sort((a, b) => a.gameDate.localeCompare(b.gameDate))
          .map((g) => (
            <li key={g.gameId}>
              <Link
                to={`/game/${g.gameId}`}
                // Phones: the date on its own small line, then the matchup and score
                className={`${linkRowClass} flex-col items-start gap-0.5 sm:flex-row sm:items-center sm:gap-3`}
              >
                <span className={`text-xs sm:text-sm sm:w-32 sm:shrink-0 ${mutedClass}`}>
                  {formatDate(g.gameDate)}
                </span>
                <span className="flex flex-wrap items-baseline gap-x-3">
                  <span className="font-medium">
                    {g.away.name} at {g.home.name}
                  </span>
                  <span className={`text-sm font-mono ${g.played ? '' : mutedClass}`}>
                    {g.played && <span className="sr-only">final </span>}
                    {g.played ? `${g.away.goals}–${g.home.goals}` : 'Upcoming'}
                  </span>
                </span>
              </Link>
            </li>
          ))}
      </ul>
    </section>
  );
}

function ArticleResults({ articles }: { articles: ArticleResult[] }) {
  return (
    <section aria-labelledby="results-articles" className={sectionClass}>
      <h2 id="results-articles" className={headingClass}>
        NHL.com articles
      </h2>
      <ul className="space-y-1">
        {articles.map((a) => (
          <li key={a.url}>
            <a
              href={a.url}
              target="_blank"
              rel="noopener noreferrer"
              className={`${linkRowClass} flex-col items-start gap-1`}
            >
              <span className="font-medium text-accent-700 dark:text-accent-400">
                {a.title}
                <span className="sr-only"> (opens NHL.com in a new tab)</span>
              </span>
              {a.snippet && (
                <span className={`text-sm ${mutedClass}`}>
                  <Highlighted text={a.snippet} />
                </span>
              )}
              {a.date && <span className={`text-xs ${mutedClass}`}>{formatDate(a.date)}</span>}
            </a>
          </li>
        ))}
      </ul>
    </section>
  );
}

function Results({ outcome, q }: { outcome: SearchOutcome; q: string }) {
  if (outcome.kind === 'off')
    return <p className={mutedClass}>Search is turned off on this server.</p>;
  if (outcome.kind === 'error')
    return (
      <p role="alert" className="text-red-700 dark:text-red-400">
        Search isn&apos;t available right now. Try again in a minute.
      </p>
    );
  if (outcome.kind === 'empty')
    return (
      <p className={mutedClass}>
        Search teams, players, games and NHL.com articles. Try &ldquo;leafs&rdquo;, &ldquo;connor
        mcdavid&rdquo;, &ldquo;bruins rangers&rdquo;, &ldquo;oilers march 3&rdquo; or
        &ldquo;trade&rdquo;.
      </p>
    );

  const { teams, players, games, articles } = outcome.results;
  if (teams.length + players.length + games.length + articles.length === 0)
    return <p className={mutedClass}>No results for &ldquo;{q}&rdquo;.</p>;
  return (
    <div className="space-y-5">
      {teams.length > 0 && <TeamResults teams={teams} />}
      {players.length > 0 && <PlayerResults players={players} />}
      {games.length > 0 && <GameResults games={games} />}
      {articles.length > 0 && <ArticleResults articles={articles} />}
    </div>
  );
}

function summary(outcome: SearchOutcome, q: string) {
  if (outcome.kind !== 'results') return '';
  const { teams, players, games, articles } = outcome.results;
  const count = teams.length + players.length + games.length + articles.length;
  return count === 0 ? `No results for ${q}` : `${count} result${count === 1 ? '' : 's'} for ${q}`;
}

export function SearchPage() {
  const { q, outcome } = useLoaderData<typeof clientLoader>();
  const [, setSearchParams] = useSearchParams();
  const navigation = useNavigation();
  const [text, setText] = useState(q);
  const [shownQ, setShownQ] = useState(q);
  const typingTimer = useRef<ReturnType<typeof setTimeout>>(undefined);

  // Back/forward or a new search changes the URL: show its query (adjusting state during render,
  // as React recommends, instead of an effect that renders twice)
  if (q !== shownQ) {
    setShownQ(q);
    if (q.trim() !== text.trim()) setText(q);
  }
  useEffect(() => () => clearTimeout(typingTimer.current), []);

  function onChange(value: string) {
    setText(value);
    clearTimeout(typingTimer.current);
    const trimmed = value.trim();
    if (trimmed === q.trim() || (trimmed.length > 0 && trimmed.length < MIN_QUERY_CHARS)) return;
    // Results follow the typing; replace keeps one history entry per search, not per keystroke
    typingTimer.current = setTimeout(
      () => setSearchParams(trimmed ? { q: trimmed } : {}, { replace: true }),
      TYPING_DELAY_MS,
    );
  }

  const searching = navigation.state === 'loading' && navigation.location?.pathname === '/search';
  // Nothing shows (or is announced) for a search that answers within LOADING_DELAY_MS
  const slow = useDelayedFlag(searching);
  // Like the navbar box, the input's icon turns into a spinner from the first keystroke until that text's
  // results arrive, so a search under way never looks like nothing happened. The status line and the first
  // search's skeleton still wait LOADING_DELAY_MS
  const typed = text.trim();
  const pending = searching || (typed !== q.trim() && typed.length >= MIN_QUERY_CHARS);
  const hasResults =
    outcome.kind === 'results' &&
    [
      outcome.results.teams,
      outcome.results.players,
      outcome.results.games,
      outcome.results.articles,
    ].some((list) => list.length > 0);

  return (
    <div className="max-w-3xl mx-auto">
      <PageTitle
        title={q ? `${q} – Search` : 'Search'}
        description="Search NHL teams, players, games and NHL.com articles."
        noindex
      />
      <h1 className="font-display text-3xl font-bold tracking-tight mb-6">Search</h1>
      {/* A plain GET form, so it works before the page's script loads. No button: results update as you type,
          and Enter (the phone keyboard's Search key) submits a one-field form without one */}
      <Form role="search" method="get" action="/search" className="mb-6 flex gap-2">
        <label htmlFor="search-page-input" className="sr-only">
          Search teams, players, games and articles
        </label>
        <div className="relative min-w-0 flex-1">
          {pending ? (
            <span
              data-loading="search-box"
              className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-accent-600 dark:text-accent-400"
            >
              <Spinner className="h-4 w-4" />
            </span>
          ) : (
            <SearchIcon className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-surface-500 dark:text-surface-400" />
          )}
          <input
            id="search-page-input"
            type="search"
            name="q"
            value={text}
            onChange={(e) => onChange(e.target.value)}
            maxLength={MAX_QUERY_CHARS}
            autoComplete="off"
            enterKeyHint="search"
            placeholder="Teams, players, games, articles…"
            className="w-full rounded-lg border border-surface-300 dark:border-white/15 bg-white/80 dark:bg-white/[0.04] pl-10 pr-4 py-2.5 text-base focus:outline-none focus-visible:ring-2 focus-visible:ring-accent-500"
          />
        </div>
      </Form>
      {/* Announces a slow search once, then the result count after each search, including ones made while typing */}
      <p role="status" className="sr-only">
        {slow ? 'Searching…' : searching ? '' : summary(outcome, q)}
      </p>
      {slow && !hasResults ? (
        // Nothing worth keeping on screen (first search, no results, an error): result-shaped placeholders
        <SearchResultsSkeleton />
      ) : (
        // The old results stay while new ones load; the box's spinner says a search is under way
        <div aria-busy={slow}>
          {/* No fade over the old results: dimmed, their muted text fails color contrast (the a11y scan checks) */}
          <Results outcome={outcome} q={q} />
        </div>
      )}
    </div>
  );
}

export default SearchPage;
