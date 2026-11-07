import { type RouteConfig, index, layout, route } from '@react-router/dev/routes';

export default [
  layout('components/Layout.tsx', [
    index('pages/GamesPage.tsx'),
    route('teams', 'pages/TeamsPage.tsx'),
    route('team/:teamId', 'pages/TeamDetailPage.tsx'),
    route('game/:gameId', 'pages/GamePage.tsx'),
    route('strategies', 'pages/CrossBookStrategyPage.tsx'),
    route('track-record', 'pages/TrackRecordPage.tsx'),
    route('disagreements', 'pages/DisagreementsPage.tsx'),
    route('about', 'pages/AboutPage.tsx'),
    route('admin', 'pages/AdminPage.tsx'),
  ]),
  route('sitemap.xml', 'routes/sitemap.ts'),
  // Link preview images (og:image) for the pages above
  route('og/game/:file', 'routes/og.game.tsx'),
  route('og/team/:file', 'routes/og.team.tsx'),
  route('og/:file', 'routes/og.page.tsx'),
] satisfies RouteConfig;
