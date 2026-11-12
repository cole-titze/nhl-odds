// Mock API data for the accessibility scan. Covers played, upcoming and
// playoff games, every job status, and a season with health check issues, so
// each page renders its badges, tables and charts.
export const THEMES = [
  'light',
  'dark',
  'ice',
  'stars',
  'nebula',
  'contrast-light',
  'contrast-dark',
  'team',
];

const isoDate = (d) => d.toISOString().slice(0, 10);
const today = new Date();
/** The day the Games page opens scrolled to */
export const ANCHOR_DATE = isoDate(today);

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

/** A retired player in the sitemap fixture: dated their last game, years back */
export const SITEMAP_RETIRED = {
  id: 8448208,
  lastGameDate: '2012-04-07T23:00:00Z',
  lastmod: '2012-04-07',
};
/** Players whose last game is in the future or unreadable: the sitemap leaves them out */
export const SITEMAP_LEFT_OUT = [8400001, 8400002];

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

// Players for the Player page, the Team page's roster and the Game page's box score
const headShot = (id) => `https://assets.nhle.com/mugs/nhl/20262027/TOR/${id}.png`;
const rosterPlayer = (
  id,
  firstName,
  lastName,
  position,
  sweaterNumber,
  gamesPlayed,
  isInjuredReserve = false,
) => ({
  id,
  firstName,
  lastName,
  headShot: headShot(id),
  position,
  sweaterNumber,
  gamesPlayed,
  isInjuredReserve,
});
const TOR_TEAM = {
  id: 10,
  locationName: 'Toronto',
  teamName: 'Maple Leafs',
  logoUri: 'https://assets.nhle.com/logos/nhl/svg/TOR_light.svg',
};
// A player on no NHL roster now (#170): the page shows no team
const retiredPlayer = {
  ...rosterPlayer(8448208, 'Jaromir', 'Retired', 'R', 68),
  birthDate: '1972-02-15',
  birthCity: 'Kladno',
  birthStateProvince: '',
  heightInInches: 75,
  weightInPounds: 230,
  currentTeam: null,
  isInjuredReserve: false,
  lastTeam: TOR_TEAM,
  lastSeasonStartYear: 2011,
  seasonStartYear: 2026,
  games: [],
  seasonTotals: {
    gamesPlayed: 0,
    timeOnIceSeconds: 0,
    goals: 0,
    assists: 0,
    points: 0,
    shotsOnGoal: 0,
    plusMinus: 0,
    shotsAgainst: 0,
    saves: 0,
    goalsAgainst: 0,
    savePct: null,
    goalsAgainstAverage: null,
  },
};
const torRoster = [
  rosterPlayer(8479318, 'Auston', 'Matthews', 'C', 34, 14),
  rosterPlayer(8478483, 'Mitch', 'Marner', 'R', 16, 14),
  rosterPlayer(8477939, 'William', 'Nylander', 'L', 88, 13),
  // On injured reserve (#170), so the roster shows the IR badge
  rosterPlayer(8476853, 'Morgan', 'Rielly', 'D', 44, 14, true),
  rosterPlayer(8479361, 'Joseph', 'Woll', 'G', 60, 9),
];
const bosRoster = [
  rosterPlayer(8477956, 'David', 'Pastrnak', 'R', 88, 14),
  rosterPlayer(8479325, 'Charlie', 'McAvoy', 'D', 73, 12),
  rosterPlayer(8480280, 'Jeremy', 'Swayman', 'G', 1, 10),
];
const skaterLine = (goals, assists, shotsOnGoal, plusMinus, timeOnIceSeconds) => ({
  timeOnIceSeconds,
  goals,
  assists,
  points: goals + assists,
  shotsOnGoal,
  plusMinus,
  shotsAgainst: 0,
  saves: 0,
  goalsAgainst: 0,
  savePct: null,
  isStarter: false,
});
const goalieLine = (saves, goalsAgainst) => ({
  timeOnIceSeconds: 3600,
  goals: 0,
  assists: 0,
  points: 0,
  shotsOnGoal: 0,
  plusMinus: 0,
  shotsAgainst: saves + goalsAgainst,
  saves,
  goalsAgainst,
  savePct: saves / (saves + goalsAgainst),
  isStarter: true,
});
const gamePlayer = (p, stats) => {
  const { headShot: _h, gamesPlayed: _g, isInjuredReserve: _ir, ...rest } = p;
  return { ...rest, stats };
};
// The Player page's games: the played ones among the team's games, newest first
const playerGames = games
  .slice(29, 41)
  .filter((g) => g.hasBeenPlayed)
  .reverse()
  .map((g, i) => {
    const won = g.winner === 0;
    return {
      gameId: g.id,
      gameDate: g.gameDate,
      gameType: g.gameType,
      teamId: 10,
      isHome: true,
      opponent: (({ id, locationName, teamName, logoUri }) => ({
        id,
        locationName,
        teamName,
        logoUri,
      }))(g.awayTeam),
      teamGoals: g.homeTeam.goals,
      opponentGoals: g.awayTeam.goals,
      result: won ? 'W' : i % 2 ? 'OTL' : 'L',
      stats: skaterLine(i % 3 === 0 ? 1 : 0, i % 2, 3 + i, won ? 1 : -1, 1150 + i * 20),
    };
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
  GetAnchorDate: { anchorDate: ANCHOR_DATE },
  // The Game page's game (pagesFor's /game/:id); without it the page rendered as TBD vs TBD.
  // The venue is its schema.org location (#167)
  GetGame: { ...games[33], venue: { name: 'Scotiabank Arena', city: 'Toronto' } },
  // Game page "Why the model predicts this" (#155): both directions, a detail and a long one
  GetPredictionFactors: {
    factors: {
      gameId: games[33].id,
      runDateUTC: `${ANCHOR_DATE}T11:00:00Z`,
      baseHomeOdds: 0.541,
      factors: [
        {
          factor: 'Rest',
          detail: 'Home 2 days since last game, away back-to-back',
          contribution: 0.031,
        },
        {
          factor: 'Goaltending',
          detail: 'Save % this season: home .903, away .917',
          contribution: -0.018,
        },
        { factor: 'Elo rating', detail: 'Elo: home 1534, away 1498', contribution: 0.012 },
        {
          factor: 'Travel',
          detail:
            'Away team travelled 1,840 km more since its last game; away team 2 time zones from home',
          contribution: 0.009,
        },
        { factor: 'Special teams', detail: '', contribution: -0.006 },
        { factor: 'Shot share', detail: 'Corsi %: home 51.2%, away 49.8%', contribution: 0.004 },
        {
          factor: 'Results',
          detail: 'Win % this season: home 58.3%, away 50.0%',
          contribution: -0.003,
        },
      ],
    },
  },
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
  // GetAllTeams' teams without their games, and each game once (both teams share the same games)
  GetSeasonTeams: {
    teams: [team(10, 'Toronto', 'Maple Leafs', 'TOR'), team(6, 'Boston', 'Bruins', 'BOS')].map(
      ({ gameOddsVM, ...t }) => t,
    ),
    seasonTotals: {
      modelLogLoss: 0.65,
      totalGameCount: 12,
      totalModelAccurateGameCount: 8,
      draftKingsLogLoss: 0.66,
      draftKingsAccurateGameCount: 7,
      draftKingsGameCount: 12,
    },
    games: games.slice(29, 41).map((g) => ({
      id: g.id,
      gameDate: g.gameDate,
      hasBeenPlayed: g.hasBeenPlayed,
      logLoss: g.logLoss,
      winner: g.winner,
      homeTeamId: g.homeTeam.id,
      awayTeamId: g.awayTeam.id,
      bookmakerOdds: g.bookmakerOdds.map(({ bookmakerName, homeOdds, awayOdds }) => ({
        bookmakerName,
        homeOdds,
        awayOdds,
      })),
    })),
    otherBookmakers: ['FanDuel'],
  },
  GetSeasonBookmakerOdds: games
    .slice(29, 41)
    .map((g) => ({ gameId: g.id, homeOdds: 0.52, awayOdds: 0.48 })),
  GetTeam: team(10, 'Toronto', 'Maple Leafs', 'TOR'),
  GetTeamRoster: torRoster,
  GetPlayer: {
    ...rosterPlayer(8479318, 'Auston', 'Matthews', 'C', 34),
    birthDate: '1997-09-17',
    birthCity: 'San Ramon',
    birthStateProvince: 'CA',
    heightInInches: 75,
    weightInPounds: 215,
    currentTeam: TOR_TEAM,
    isInjuredReserve: false,
    lastTeam: TOR_TEAM,
    lastSeasonStartYear: 2026,
    seasonStartYear: 2026,
    games: playerGames,
    seasonTotals: {
      gamesPlayed: playerGames.length,
      timeOnIceSeconds: playerGames.reduce((t, g) => t + g.stats.timeOnIceSeconds, 0),
      goals: playerGames.reduce((t, g) => t + g.stats.goals, 0),
      assists: playerGames.reduce((t, g) => t + g.stats.assists, 0),
      points: playerGames.reduce((t, g) => t + g.stats.points, 0),
      shotsOnGoal: playerGames.reduce((t, g) => t + g.stats.shotsOnGoal, 0),
      plusMinus: playerGames.reduce((t, g) => t + g.stats.plusMinus, 0),
      shotsAgainst: 0,
      saves: 0,
      goalsAgainst: 0,
      savePct: null,
      goalsAgainstAverage: null,
    },
  },
  // The Game page's game (games[33]) was played, so it gets a box score
  GetGamePlayers: {
    gameId: games[33].id,
    hasStats: true,
    home: torRoster.map((p) =>
      gamePlayer(p, p.position === 'G' ? goalieLine(28, 3) : skaterLine(1, 0, 4, 1, 1100)),
    ),
    away: bosRoster.map((p) =>
      gamePlayer(p, p.position === 'G' ? goalieLine(30, 3) : skaterLine(0, 1, 3, -1, 1200)),
    ),
  },
  // The Leaders page (#182): the mock answers every stat with the same ranked skaters, a tie
  // included, and more than the 10 shown so the "Show top" button renders
  GetStatLeaders: {
    seasonStartYear: 2026,
    stat: 'points',
    label: 'Points',
    group: 'skater',
    lowerIsBetter: false,
    minGamesPlayed: 0,
    leaders: [...torRoster, ...bosRoster, ...torRoster, ...bosRoster]
      .filter((p) => p.position !== 'G')
      .map((p, i) => ({
        rank: i === 2 ? 2 : i + 1,
        playerId: p.id + i,
        firstName: p.firstName,
        lastName: p.lastName,
        headShot: p.headShot,
        position: p.position,
        teamId: i % 2 ? 6 : 10,
        teamAbbreviation: i % 2 ? 'BOS' : 'TOR',
        teamName: i % 2 ? 'Boston Bruins' : 'Toronto Maple Leafs',
        teamLogoUri: '/favicon.svg',
        gamesPlayed: 14,
        value: i === 2 ? 20 : 22 - i,
      })),
  },
  // Everyone's last game (#171): this season's rosters, a retired player, and
  // two bad rows the sitemap must leave out (SITEMAP_LEFT_OUT)
  GetSitemapPlayers: [
    ...[...torRoster, ...bosRoster].map((p) => ({ id: p.id, lastGameDate: games[34].gameDate })),
    { id: SITEMAP_RETIRED.id, lastGameDate: SITEMAP_RETIRED.lastGameDate },
    { id: SITEMAP_LEFT_OUT[0], lastGameDate: games[70].gameDate },
    { id: SITEMAP_LEFT_OUT[1], lastGameDate: 'not a date' },
  ],
  GetBestStrategies: [
    {
      betType: 'moneyline',
      strategyType: 'value',
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
    nextDayKalshi: job('requested'),
    kalshiBackfill: job('idle'),
    searchIndex: job('idle'),
    loadTestLight: job('completed'),
    loadTestHeavy: job('idle'),
    loadTestStress: job('idle'),
    loadTestMax: job('idle'),
  },
  // Every bar state: up, some downtime, major downtime, and days with no data
  GetUptimeDays: Array.from({ length: 25 }, (_, i) => {
    const d = new Date(today);
    d.setUTCDate(d.getUTCDate() - 24 + i);
    const down = i === 5 ? 5 : i === 12 ? 60 : 0;
    return {
      day: isoDate(d),
      expectedProbes: 1440,
      probes: i === 20 ? 1300 : 1440,
      successfulProbes: (i === 20 ? 1300 : 1440) - down,
      avgResponseMs: 55 + (i % 7),
    };
  }),
  GetLoadTestRuns: [
    {
      id: 1,
      preset: 'light',
      target: 'http://nhl-odds-frontend',
      maxVus: 25,
      rampUpSeconds: 30,
      holdSeconds: 120,
      rampDownSeconds: 30,
      thinkTimeMinSeconds: 1,
      thinkTimeMaxSeconds: 3,
      pageMix: '/ 35%, /game/:id 25%, /team/:id 15%, /teams 10%, /strategies 10%, /about 5%',
      startedUTC: '2026-10-13T15:00:00',
      finishedUTC: '2026-10-13T15:03:05',
      requests: 2310,
      requestsPerSecond: 12.5,
      p50Ms: 48.2,
      p95Ms: 212.7,
      p99Ms: 640.1,
      maxMs: 1520.4,
      http5xx: 0,
      http429: 2,
      otherErrors: 0,
      pages: [
        {
          page: '/',
          requests: 810,
          requestsPerSecond: 6.5,
          p50Ms: 40.1,
          p95Ms: 150.2,
          p99Ms: 380.5,
          maxMs: 900.3,
          http5xx: 0,
          http429: 0,
          otherErrors: 0,
        },
        {
          page: '/game/:id',
          requests: 580,
          requestsPerSecond: 4.6,
          p50Ms: 62.4,
          p95Ms: 260.8,
          p99Ms: 640.1,
          maxMs: 1520.4,
          http5xx: 0,
          http429: 1,
          otherErrors: 0,
        },
        {
          page: '/team/:id',
          requests: 350,
          requestsPerSecond: 2.8,
          p50Ms: 55.0,
          p95Ms: 210.3,
          p99Ms: 500.2,
          maxMs: 1100.0,
          http5xx: 0,
          http429: 1,
          otherErrors: 0,
        },
        {
          page: '/teams',
          requests: 230,
          requestsPerSecond: 1.8,
          p50Ms: 48.7,
          p95Ms: 190.0,
          p99Ms: 420.9,
          maxMs: 800.1,
          http5xx: 0,
          http429: 0,
          otherErrors: 0,
        },
        {
          page: '/strategies',
          requests: 225,
          requestsPerSecond: 1.8,
          p50Ms: 20.2,
          p95Ms: 60.4,
          p99Ms: 120.0,
          maxMs: 210.5,
          http5xx: 0,
          http429: 0,
          otherErrors: 0,
        },
        {
          page: '/about',
          requests: 115,
          requestsPerSecond: 0.9,
          p50Ms: 12.3,
          p95Ms: 30.1,
          p99Ms: 55.2,
          maxMs: 90.4,
          http5xx: 0,
          http429: 0,
          otherErrors: 0,
        },
      ],
    },
  ],
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
  GetGameLineup: {
    lineup: {
      gameId: 2026020100,
      updatedUTC: new Date(today.getTime() - 3 * 3600000).toISOString(),
      versionCount: 2,
      statusReport:
        'Matthews took part in the morning skate and is expected to play.\nSwayman will start after Korpisalo started on Tuesday.',
      away: {
        // Forwards and the first goalie are matched to player pages (links); the rest stay unmatched (plain
        // text), so the scan covers both
        forwards: [
          ['Knies', 'Matthews', 'Marner'],
          ['Robertson', 'Tavares', 'Nylander'],
          ['McMann', 'Domi', 'Jarnkrok'],
          ['Lorentz', 'Kampf', 'Reaves'],
        ].map((line, i) =>
          line.map((name, j) => ({ name, playerId: 8470000 + i * 10 + j, note: null })),
        ),
        defense: [
          ['Rielly', 'Tanev'],
          ['McCabe', 'Ekman-Larsson'],
          ['Benoit', 'Myers'],
        ].map((pair) => pair.map((name) => ({ name, playerId: null, note: null }))),
        goalies: [
          { name: 'Stolarz', playerId: 8476932, note: null },
          { name: 'Woll', playerId: null, note: null },
        ],
        scratched: [{ name: 'Timmins', playerId: null, note: null }],
        injured: [{ name: 'Liljegren', playerId: null, note: 'lower body' }],
        suspended: [],
      },
      home: {
        forwards: [['Marchand', 'Zacha', 'Pastrnak']].map((line) =>
          line.map((name) => ({ name, playerId: null, note: null })),
        ),
        defense: [['Lindholm', 'McAvoy']].map((pair) =>
          pair.map((name) => ({ name, playerId: null, note: null })),
        ),
        goalies: [{ name: 'Swayman', playerId: null, note: null }],
        scratched: [],
        injured: [],
        suspended: [{ name: 'Lauko', playerId: null, note: null }],
      },
    },
  },
  GetOddsMovement: (() => {
    const start = new Date(today.getTime() + 6 * 3600000);
    const at = (hoursBefore) => new Date(start.getTime() - hoursBefore * 3600000).toISOString();
    const points = (pairs) => pairs.map(([h, p]) => ({ timeUTC: at(h), homeProbability: p }));
    return {
      gameId: 2026020100,
      startUTC: start.toISOString(),
      series: [
        { name: 'Model', kind: 'model', points: points([[20, 0.58]]) },
        {
          name: 'DraftKings (US)',
          kind: 'partner',
          points: points([
            [16, 0.52],
            [9, 0.535],
            [4, 0.55],
            [1, 0.556],
          ]),
        },
        {
          name: 'FanDuel (CA)',
          kind: 'partner',
          points: points([
            [15, 0.515],
            [6, 0.545],
          ]),
        },
        {
          name: "Sportsbooks' average",
          kind: 'average',
          points: points([
            [18, 0.52],
            [0.02, 0.553],
          ]),
        },
        {
          name: 'Kalshi',
          kind: 'kalshi',
          points: points([
            [18, 0.51],
            [0.02, 0.56],
          ]),
        },
      ],
      move: { open: 0.52, close: 0.553, model: 0.58, towardModel: 0.033 },
    };
  })(),
  GetTrackRecord: {
    seasonStartYear: 2026,
    liveOnly: false,
    gameCount: 120,
    liveGameCount: 90,
    modelLogLoss: 0.6712,
    modelAccuracy: 0.575,
    calibration: [
      { low: 0.3, high: 0.35, games: 6, meanPredicted: 0.33, homeWinRate: 0.33 },
      { low: 0.4, high: 0.45, games: 22, meanPredicted: 0.43, homeWinRate: 0.41 },
      { low: 0.5, high: 0.55, games: 40, meanPredicted: 0.52, homeWinRate: 0.55 },
      { low: 0.6, high: 0.65, games: 30, meanPredicted: 0.62, homeWinRate: 0.6 },
      { low: 0.7, high: 0.75, games: 22, meanPredicted: 0.71, homeWinRate: 0.77 },
    ],
    logLossByDate: Array.from({ length: 20 }, (_, i) => ({
      date: isoDate(new Date(today.getTime() - (20 - i) * 86400000)) + 'T00:00:00',
      games: (i + 1) * 6,
      model: 0.68 - i * 0.0005,
      market: 0.675 - i * 0.0004,
    })),
    sources: [
      {
        name: "Bookmakers' average (morning)",
        games: 110,
        logLoss: 0.668,
        accuracy: 0.58,
        modelLogLoss: 0.671,
        modelAccuracy: 0.57,
      },
      {
        name: "Bookmakers' average (closing)",
        games: 100,
        logLoss: 0.664,
        accuracy: 0.59,
        modelLogLoss: 0.67,
        modelAccuracy: 0.57,
      },
      {
        name: 'DraftKings',
        games: 110,
        logLoss: 0.669,
        accuracy: 0.58,
        modelLogLoss: 0.671,
        modelAccuracy: 0.57,
      },
      {
        name: 'Kalshi',
        games: 95,
        logLoss: 0.673,
        accuracy: 0.56,
        modelLogLoss: 0.67,
        modelAccuracy: 0.57,
      },
    ],
    closingLine: {
      games: 100,
      modelCloser: 54,
      movedTowardModel: 51,
      movedAwayFromModel: 44,
      averageMoveTowardModel: 0.0042,
    },
  },
};

const searchResults = {
  query: 'leafs',
  teams: [
    {
      teamId: 10,
      name: 'Toronto Maple Leafs',
      abbreviation: 'TOR',
      logoUri: 'https://assets.nhle.com/logos/nhl/svg/TOR_light.svg',
      active: true,
    },
    {
      teamId: 53,
      name: 'Arizona Coyotes',
      abbreviation: 'ARI',
      logoUri: 'https://assets.nhle.com/logos/nhl/svg/ARI_light.svg',
      active: false,
    },
  ],
  players: [
    {
      playerId: 8479318,
      name: 'Auston Matthews',
      position: 'C',
      teamId: 10,
      teamName: 'Toronto Maple Leafs',
      teamAbbreviation: 'TOR',
      isActive: true,
      headshot: '',
    },
    {
      playerId: 1,
      name: 'Free Agent',
      position: null,
      teamId: null,
      teamName: null,
      teamAbbreviation: null,
      isActive: false,
      headshot: '',
    },
  ],
  games: [
    {
      gameId: 2026020100,
      gameDate: '2026-10-31',
      startTimeUtc: '2026-10-31T23:00:00Z',
      played: true,
      home: { teamId: 10, name: 'Toronto Maple Leafs', abbreviation: 'TOR', goals: 4 },
      away: { teamId: 5, name: 'Pittsburgh Penguins', abbreviation: 'PIT', goals: 2 },
    },
    {
      gameId: 2026020200,
      gameDate: '2026-11-17',
      startTimeUtc: '2026-11-18T00:00:00Z',
      played: false,
      home: { teamId: 13, name: 'Florida Panthers', abbreviation: 'FLA', goals: null },
      away: { teamId: 10, name: 'Toronto Maple Leafs', abbreviation: 'TOR', goals: null },
    },
  ],
  articles: [
    {
      title: 'NHL Trade Tracker',
      summary: 'Every deal',
      url: 'https://www.nhl.com/news/nhl-trade-tracker',
      date: '2026-11-02T18:00:00Z',
      snippet: 'The Maple \uE000Leafs\uE001 acquired a defenseman from the Sharks.',
    },
  ],
};

export function mockApi(url) {
  if (url.includes('/api/Search/Suggest')) {
    const { query, teams, players, games } = searchResults;
    return { query: new URL(url).searchParams.get('q') ?? query, teams, players, games };
  }
  if (url.includes('/api/Search')) return searchResults;
  if (url.includes('/GetGameOddsInDateRange')) {
    const params = new URL(url).searchParams;
    const start = params.get('startDate');
    const end = params.get('endDate');
    return games.filter((g) => g.gameDate.slice(0, 10) >= start && g.gameDate.slice(0, 10) <= end);
  }
  // By the action's exact name: GetTeam must not answer GetTeamRoster
  const action = new URL(url).pathname.split('/').pop();
  if (
    action === 'GetPlayer' &&
    new URL(url).searchParams.get('playerId') === String(retiredPlayer.id)
  )
    return retiredPlayer;
  return Object.hasOwn(responses, action) ? responses[action] : {};
}

// One page of each type. The Lighthouse check (lighthouse/run.mjs) scores the same list, minus /admin
export function pagesFor() {
  return [
    '/',
    '/teams',
    '/team/10',
    `/game/${games[33].id}`,
    '/player/8479318',
    `/player/${retiredPlayer.id}`,
    '/strategies',
    '/track-record',
    '/leaders',
    '/about',
    '/admin',
    '/search?q=leafs',
  ];
}
