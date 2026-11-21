import { redirect } from 'react-router';

// Readable page URLs: the id, then the names, e.g. /player/8478402-connor-mcdavid and
// /game/2026020123-oilers-at-maple-leafs. Only the id counts when a page loads; a URL
// with other words (or none, as old links have) redirects to the one these build.

// Letters NFKD doesn't split into a plain letter and an accent
const LETTERS: Record<string, string> = {
  ø: 'o',
  æ: 'ae',
  œ: 'oe',
  ß: 'ss',
  ł: 'l',
  đ: 'd',
  ð: 'd',
  þ: 'th',
};

/** "Tim Stützle" → "tim-stutzle", "Ryan O'Reilly" → "ryan-oreilly" */
export function slugify(text: string): string {
  return text
    .toLowerCase()
    .normalize('NFKD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[øæœßłđðþ]/g, (c) => LETTERS[c])
    .replace(/['’.]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function withSlug(base: string, id: number, words: string): string {
  const slug = slugify(words);
  return slug ? `${base}/${id}-${slug}` : `${base}/${id}`;
}

type PlayerNames = { firstName: string; lastName: string };

/** A player's page; `name` is "First Last" or the two parts. Without it, the id-only URL (which redirects). */
export function playerPath(id: number, name?: string | PlayerNames | null): string {
  const words = typeof name === 'string' ? name : name ? `${name.firstName} ${name.lastName}` : '';
  return withSlug('/player', id, words);
}

/** A game's page, from the teams' names ("Oilers", or "Edmonton Oilers"); a team still TBD leaves the names out */
export function gamePath(id: number, awayName?: string | null, homeName?: string | null): string {
  return withSlug('/game', id, awayName && homeName ? `${awayName} at ${homeName}` : '');
}

/** The page path for a game with its matchup teams (GameOddsVM) */
export const gamePagePath = (game: {
  id: number;
  awayTeam: { teamName: string } | null;
  homeTeam: { teamName: string } | null;
}) => gamePath(game.id, game.awayTeam?.teamName, game.homeTeam?.teamName);

/** The id at the start of a :gameId / :playerId route param ("8478402-connor-mcdavid" → "8478402"); other params as they are */
export function idParam(param: string | undefined): string | undefined {
  return param?.match(/^(\d+)(?:-|$)/)?.[1] ?? param;
}

/**
 * For a loader: a permanent redirect to the page's own path when the request came in on another one
 * (an old id-only link, a renamed team), keeping the query string; otherwise null.
 */
export function redirectToPath(request: Request, path: string): Response | null {
  const url = new URL(request.url);
  return url.pathname === path ? null : redirect(path + url.search, 301);
}
