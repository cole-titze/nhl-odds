// /og/{games,teams,strategies,about,site}.png: link previews for the pages that aren't
// one game or team. Games shows the day the page opens on; Teams the season's totals.
import type { LoaderFunctionArgs } from 'react-router';
import { getAnchorDate, getGameOddsInDateRange } from '../api/gameOdds';
import { getSeasonTeams } from '../api/teams';
import { GamesCard, PageCard, TeamsCard, type SlateGame } from '../og/cards';
import { imageResponse, logoDataUri } from '../og/render.server';
import { addDaysIso, formatDate } from '../utils/dates';
import { getCurrentSeason } from '../utils/season';

const HOUR = 60 * 60;

const PAGES: Record<string, { label?: string; title: string; text: string }> = {
  strategies: {
    label: 'Strategies',
    title: 'Betting strategies, backtested',
    text: "The model's win probabilities against sportsbook and Kalshi odds, season by season.",
  },
  about: {
    label: 'About',
    title: 'How NHL Odds works',
    text: 'A machine learning model trained on NHL play-by-play data since 2009.',
  },
  site: {
    title: 'NHL Odds',
    text: 'Machine learning win probabilities for every NHL game, compared with sportsbook and Kalshi odds.',
  },
};

// The game day in Central time, like the Games page's server render
const centralDay = (iso: string) =>
  new Date(iso).toLocaleDateString('en-CA', { timeZone: 'America/Chicago' });

async function gamesImage(request: Request) {
  const season = getCurrentSeason();
  const { anchorDate } = await getAnchorDate(season);
  const day = anchorDate ?? formatDate(new Date());
  // Evening games in Central time are the next day in UTC
  const games = (await getGameOddsInDateRange(day, addDaysIso(day, 1), season))
    .filter((g) => g.homeTeam && g.awayTeam && centralDay(g.gameDate) === day)
    .sort((a, b) => a.gameDate.localeCompare(b.gameDate));
  if (games.length === 0) return pageImage(request, 'site', 15 * 60);

  const slate: SlateGame[] = games.map((g) => ({
    away: g.awayTeam!,
    home: g.homeTeam!,
    played: g.hasBeenPlayed,
  }));
  const uris = [...new Set(slate.flatMap((g) => [g.away.logoUri, g.home.logoUri]))];
  const logos = Object.fromEntries(
    await Promise.all(uris.map(async (u) => [u, await logoDataUri(u)] as const)),
  );
  const today = formatDate(new Date());
  const dayLabel =
    day === today
      ? 'Today'
      : new Date(`${day}T12:00:00`).toLocaleDateString('en-US', {
          weekday: 'long',
          month: 'long',
          day: 'numeric',
        });
  const drawn = {
    v: 1,
    dayLabel,
    games: slate.map((g) => [
      g.away.logoUri,
      g.home.logoUri,
      g.played,
      g.away.modelOdds,
      g.home.modelOdds,
      g.away.goals,
      g.home.goals,
    ]),
    logos: uris.map((u) => logos[u] != null),
  };
  return imageResponse(
    request,
    drawn,
    () => <GamesCard dayLabel={dayLabel} games={slate} logos={logos} />,
    15 * 60,
  );
}

async function teamsImage(request: Request) {
  const season = getCurrentSeason();
  const { seasonTotals: totals } = await getSeasonTeams(season);
  return imageResponse(
    request,
    { v: 1, season, totals },
    () => <TeamsCard season={season} totals={totals} />,
    HOUR,
  );
}

function pageImage(request: Request, name: string, maxAge = 24 * HOUR) {
  const page = PAGES[name];
  return imageResponse(request, { v: 1, page }, () => <PageCard {...page} />, maxAge);
}

export async function loader({ params, request }: LoaderFunctionArgs) {
  const name = params.file?.match(/^([a-z]+)\.png$/)?.[1] ?? '';
  try {
    if (name === 'games') return await gamesImage(request);
    if (name === 'teams') return await teamsImage(request);
  } catch {
    // The API is down: still answer with the site's card rather than a broken image
    return pageImage(request, 'site', 5 * 60);
  }
  if (PAGES[name]) return pageImage(request, name);
  return new Response('Not found', { status: 404 });
}
