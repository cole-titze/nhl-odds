import { type RouteConfig, index, layout, route } from '@react-router/dev/routes';

export default [
  layout('components/Layout.tsx', [
    index('pages/GamesPage.tsx'),
    route('teams', 'pages/TeamsPage.tsx'),
    route('team/:teamId', 'pages/TeamDetailPage.tsx'),
    route('game/:gameId', 'pages/GamePage.tsx'),
    route('strategies', 'pages/CrossBookStrategyPage.tsx'),
    route('about', 'pages/AboutPage.tsx'),
    route('admin', 'pages/AdminPage.tsx'),
  ]),
] satisfies RouteConfig;
