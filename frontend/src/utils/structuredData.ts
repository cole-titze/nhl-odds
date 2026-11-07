// schema.org structured data (JSON-LD) for search engines (#167). PageTitle puts
// the site-wide WebSite and Organization, plus a WebPage for the page, on every
// indexable page; pages add their own items (a SportsEvent, an FAQPage, ...)
// through PageTitle's `schema` prop, built with the helpers here.
import type { GameOddsVM, MatchupTeamVM } from '../types';
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

export function sportsTeam(team: {
  id: number;
  locationName: string;
  teamName: string;
  logoUri?: string;
}) {
  return {
    '@type': 'SportsTeam',
    name: `${team.locationName} ${team.teamName}`,
    sport: 'Ice hockey',
    url: absoluteUrl(`/team/${team.id}`),
    ...(team.logoUri ? { logo: team.logoUri } : {}),
    memberOf: NHL,
  };
}

const matchupName = (home: MatchupTeamVM, away: MatchupTeamVM) =>
  `${away.locationName} ${away.teamName} at ${home.locationName} ${home.teamName}`;

/** A game as a SportsEvent, with the final score once it has been played */
export function sportsEvent(game: GameOddsVM): JsonLd | null {
  const { homeTeam, awayTeam } = game;
  if (!homeTeam || !awayTeam) return null;
  const home = sportsTeam(homeTeam);
  const away = sportsTeam(awayTeam);
  return {
    '@type': 'SportsEvent',
    name: matchupName(homeTeam, awayTeam),
    url: absoluteUrl(`/game/${game.id}`),
    sport: 'Ice hockey',
    startDate: game.gameDate,
    eventStatus: 'https://schema.org/EventScheduled',
    homeTeam: home,
    awayTeam: away,
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
