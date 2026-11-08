// schema.org structured data (JSON-LD) for search engines (#167). PageTitle puts
// the site-wide WebSite and Organization, plus a WebPage for the page, on every
// indexable page; pages add their own items (a SportsEvent, an FAQPage, ...)
// through PageTitle's `schema` prop, built with the helpers here.
//
// Teams, players and games carry an `@id` that is their page's canonical URL
// (#169), so the same entity marked up on different pages (a player on his own
// page, on his team's roster and in a game) is recognisably one thing.
import type { GameOddsVM, GamePlayersVM, MatchupTeamVM, PlayerVM } from '../types';
import { SITE_URL } from './site';

export type JsonLd = Record<string, unknown>;

const SITE_NAME = 'NHL Odds';
const WEBSITE_ID = `${SITE_URL}/#website`;
const ORGANIZATION_ID = `${SITE_URL}/#organization`;
const NHL = {
  '@type': 'SportsOrganization',
  name: 'National Hockey League',
  url: 'https://www.nhl.com',
};

export const absoluteUrl = (path: string) => SITE_URL + (path === '/' ? '/' : path);

/** The whole page's graph: the site, its publisher, this page and the page's own items */
export function pageGraph(
  page: { url: string; name: string; description: string },
  items: JsonLd[],
) {
  return {
    '@context': 'https://schema.org',
    '@graph': [
      {
        '@type': 'WebSite',
        '@id': WEBSITE_ID,
        name: SITE_NAME,
        url: absoluteUrl('/'),
        publisher: { '@id': ORGANIZATION_ID },
      },
      {
        '@type': 'Organization',
        '@id': ORGANIZATION_ID,
        name: SITE_NAME,
        url: absoluteUrl('/'),
        logo: absoluteUrl('/favicon.svg'),
      },
      {
        '@type': 'WebPage',
        '@id': `${page.url}#webpage`,
        url: page.url,
        name: page.name,
        description: page.description,
        isPartOf: { '@id': WEBSITE_ID },
      },
      ...items,
    ],
  };
}

/** JSON for a <script type="application/ld+json">; `<` is escaped so no string can close the tag */
export const jsonLdString = (data: unknown) => JSON.stringify(data).replace(/</g, '\\u003c');

export function breadcrumbs(items: { name: string; path: string }[]): JsonLd {
  return {
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, i) => ({
      '@type': 'ListItem',
      position: i + 1,
      name: item.name,
      item: absoluteUrl(item.path),
    })),
  };
}

export const teamUrl = (id: number) => absoluteUrl(`/team/${id}`);
export const playerUrl = (id: number) => absoluteUrl(`/player/${id}`);
export const gameUrl = (id: number) => absoluteUrl(`/game/${id}`);

type PersonRef = { id: number; firstName: string; lastName: string };

/** A player as a short Person: enough to name him and point at his page */
export function personRef(player: PersonRef): JsonLd {
  const url = playerUrl(player.id);
  return { '@type': 'Person', '@id': url, name: `${player.firstName} ${player.lastName}`, url };
}

/** A team, with its players as `athlete`s when they're known */
export function sportsTeam(
  team: {
    id: number;
    locationName: string;
    teamName: string;
    logoUri?: string;
  },
  athletes: PersonRef[] = [],
): JsonLd {
  const url = teamUrl(team.id);
  return {
    '@type': 'SportsTeam',
    '@id': url,
    name: `${team.locationName} ${team.teamName}`,
    sport: 'Ice hockey',
    url,
    ...(team.logoUri ? { logo: team.logoUri } : {}),
    memberOf: NHL,
    ...(athletes.length ? { athlete: athletes.map(personRef) } : {}),
  };
}

/** The Player page's Person, a member of their current team (none when they're on no NHL roster) */
export function person(player: PlayerVM): JsonLd {
  const birthPlace = [player.birthCity, player.birthStateProvince].filter(Boolean).join(', ');
  return {
    ...personRef(player),
    givenName: player.firstName,
    familyName: player.lastName,
    ...(player.headShot ? { image: player.headShot } : {}),
    ...(player.birthDate ? { birthDate: player.birthDate } : {}),
    ...(birthPlace ? { birthPlace: { '@type': 'Place', name: birthPlace } } : {}),
    ...(player.heightInInches > 0
      ? { height: { '@type': 'QuantitativeValue', value: player.heightInInches, unitCode: 'INH' } }
      : {}),
    ...(player.weightInPounds > 0
      ? { weight: { '@type': 'QuantitativeValue', value: player.weightInPounds, unitCode: 'LBR' } }
      : {}),
    ...(player.currentTeam ? { memberOf: sportsTeam(player.currentTeam) } : {}),
  };
}

const matchupName = (home: MatchupTeamVM, away: MatchupTeamVM) =>
  `${away.locationName} ${away.teamName} at ${home.locationName} ${home.teamName}`;

/**
 * A game as a SportsEvent, with the final score once it has been played. The
 * teams list their players when they're known (the box score, or the rosters
 * before the game).
 */
export function sportsEvent(game: GameOddsVM, players?: GamePlayersVM | null): JsonLd | null {
  const { homeTeam, awayTeam } = game;
  if (!homeTeam || !awayTeam) return null;
  const home = sportsTeam(homeTeam, players?.home);
  const away = sportsTeam(awayTeam, players?.away);
  const url = gameUrl(game.id);
  return {
    '@type': 'SportsEvent',
    '@id': url,
    name: matchupName(homeTeam, awayTeam),
    url,
    sport: 'Ice hockey',
    startDate: game.gameDate,
    eventStatus: 'https://schema.org/EventScheduled',
    homeTeam: home,
    awayTeam: away,
    competitor: [{ '@id': away['@id'] }, { '@id': home['@id'] }],
    superEvent: NHL,
    ...(game.hasBeenPlayed
      ? {
          description: `Final: ${awayTeam.teamName} ${awayTeam.goals}, ${homeTeam.teamName} ${homeTeam.goals}`,
        }
      : {}),
  };
}

export function itemList(name: string, items: { name: string; path: string }[]): JsonLd {
  return {
    '@type': 'ItemList',
    name,
    numberOfItems: items.length,
    itemListElement: items.map((item, i) => ({
      '@type': 'ListItem',
      position: i + 1,
      name: item.name,
      url: absoluteUrl(item.path),
    })),
  };
}

export function faqPage(questions: { question: string; answer: string }[]): JsonLd {
  return {
    '@type': 'FAQPage',
    mainEntity: questions.map((q) => ({
      '@type': 'Question',
      name: q.question,
      acceptedAnswer: { '@type': 'Answer', text: q.answer },
    })),
  };
}
