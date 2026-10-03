// Mock API data for the accessibility scan. Covers played, upcoming and
// playoff games, every job status, and a season with health check issues, so
// each page renders its badges, tables and charts.
export const THEMES = [
  'light',
  'dark',
  'ice',
  'stars',
  'aurora',
  'contrast-light',
  'contrast-dark',
  'team',
];

const isoDate = (d) => d.toISOString().slice(0, 10);
const today = new Date();

const matchupTeam = (id, locationName, teamName, abbrev, modelOdds, goals) => ({
  id,
  locationName,
  teamName,
  logoUri: `https://assets.nhle.com/logos/nhl/svg/${abbrev}_light.svg`,
  modelOdds,
  goals,
  team: 0,
});

const bookmaker = (bookmakerName, homeOdds) => ({
  bookmakerName,
  homeOdds,
  awayOdds: 1 - homeOdds,
  homePoint: -1.5,
  homePrice: 150,
  awayPoint: 1.5,
  awayPrice: -170,
  overUnderPoint: 6.5,
  overPrice: -110,
  underPrice: -110,
});

// Five weeks of games either side of today, so the Games feed can load older
// and newer chunks the way it does against the real API
const games = [];
for (let offset = -35; offset <= 35; offset++) {
  const date = new Date(today);
  date.setDate(date.getDate() + offset);
  const played = offset < 0;
  games.push({
    id: 2026020100 + offset,
    gameDate: `${isoDate(date)}T23:00:00Z`,
    homeTeam: matchupTeam(10, 'Toronto', 'Maple Leafs', 'TOR', 0.58, played ? 3 : 0),
    awayTeam: matchupTeam(6, 'Boston', 'Bruins', 'BOS', 0.42, played ? (offset % 2 ? 4 : 1) : 0),
    winner: offset % 2 ? 1 : 0,
    hasBeenPlayed: played,
    gameType: offset === 2 ? 3 : 2,
    logLoss: played ? 0.6 + offset * 0.01 : null,
    modelId: 1,
    bookmakerOdds: [bookmaker('DraftKings', 0.5), bookmaker('Kalshi', 0.47)],
    predictedSpread: 0.6,
    spreadCoverProb: 0.55,
    predictedTotal: 6.1,
    totalOverProb: 0.45,
  });
}

const team = (id, locationName, teamName, abbrev) => ({
  id,
  locationName,
  teamName,
  logoUri: `https://assets.nhle.com/logos/nhl/svg/${abbrev}_light.svg`,
  modelLogLoss: 0.65,
  totalGameCount: 6,
  seasonWins: 4,
  seasonLosses: 2,
  seasonOvertimeLosses: 0,
  totalModelAccurateGameCount: 4,
  draftKingsLogLoss: 0.66,
  draftKingsAccurateGameCount: 3,
  draftKingsGameCount: 6,
  gameOddsVM: games.slice(29, 41),
});

const job = (status) => ({
  id: status,
  name: status,
  status,
  startedAt: '2026-10-01T10:00:00',
  finishedAt: status === 'completed' || status === 'failed' ? '2026-10-01T10:05:00' : null,
  error: status === 'failed' ? 'Something went wrong' : null,
  output: 'job output',
  completedToday: status === 'completed',
});

const responses = {
  GetAnchorDate: { anchorDate: isoDate(today) },
  // Filtered by date range below
  GetAllTeams: {
    teams: [team(10, 'Toronto', 'Maple Leafs', 'TOR'), team(6, 'Boston', 'Bruins', 'BOS')],
    seasonTotals: {
      modelLogLoss: 0.65,
      totalGameCount: 12,
      totalModelAccurateGameCount: 8,
      draftKingsLogLoss: 0.66,
      draftKingsAccurateGameCount: 7,
      draftKingsGameCount: 12,
    },
  },
  GetTeam: team(10, 'Toronto', 'Maple Leafs', 'TOR'),
  GetBestStrategies: [
    {
      betType: 'moneyline',
      strategyType: 'valueBets',
      threshold: 0.05,
      bets: 100,
      wins: 55,
      roi: 4.2,
      firstSeason: 2022,
      lastSeason: 2025,
    },
  ],
  GetJobStatuses: {
    dataCollection: job('completed'),
    oddsFetch: job('running'),
    prediction: job('failed'),
    oddsBackfill: job('idle'),
    predictionBackfill: job('idle'),
    kalshiFetch: job('requested'),
    kalshiBackfill: job('idle'),
  },
  GetHealthChecks: [
    {
      seasonStartYear: 2025,
      totalGames: 1312,
      playedGames: 1312,
      missingPredictions: 0,
      missingBookmakerOdds: 3,
      missingGameCleaned: 0,
      missingOddsFetchDays: 0,
      liveBookmakerOdds: 0,
      missingKalshiOdds: -1,
      errorCount: 2,
    },
  ],
  GetErrorLogs: [],
};

export function mockApi(url) {
  if (url.includes('/GetGameOddsInDateRange')) {
    const params = new URL(url).searchParams;
    const start = params.get('startDate');
    const end = params.get('endDate');
    return games.filter((g) => g.gameDate.slice(0, 10) >= start && g.gameDate.slice(0, 10) <= end);
  }
  const key = Object.keys(responses).find((k) => url.includes(`/${k}`));
  return key ? responses[key] : {};
}

export function pagesFor() {
  return ['/', '/teams', '/team/10', `/game/${games[33].id}`, '/strategies', '/about', '/admin'];
}
