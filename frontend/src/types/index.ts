export const Winner = {
  HOME: 0,
  AWAY: 1,
} as const;

export type Winner = (typeof Winner)[keyof typeof Winner];

export interface MatchupTeamVM {
  id: number;
  locationName: string;
  teamName: string;
  logoUri: string;
  modelOdds: number | null;
  goals: number;
  team: Winner;
}

export interface BookmakerOddsVM {
  bookmakerName: string;
  homeOdds: number;
  awayOdds: number;
  homePoint: number;
  homePrice: number;
  awayPoint: number;
  awayPrice: number;
  overUnderPoint: number;
  overPrice: number;
  underPrice: number;
}

export interface GameOddsVM {
  id: number;
  gameDate: string;
  homeTeam: MatchupTeamVM | null;
  awayTeam: MatchupTeamVM | null;
  winner: Winner;
  hasBeenPlayed: boolean;
  gameType: number;
  logLoss: number | null;
  modelId: number | null;
  bookmakerOdds: BookmakerOddsVM[];
  predictedSpread: number | null;
  spreadCoverProb: number | null;
  predictedTotal: number | null;
  totalOverProb: number | null;
  /** The arena; only the single-game endpoint (the Game page) sends it */
  venue?: VenueVM;
}

export interface VenueVM {
  name: string;
  city: string;
}

export interface SeasonTotalsVM {
  modelLogLoss: number;
  totalGameCount: number;
  totalModelAccurateGameCount: number;
  draftKingsLogLoss: number;
  draftKingsAccurateGameCount: number;
  draftKingsGameCount: number;
}

export interface TeamVM {
  id: number;
  locationName: string;
  teamName: string;
  logoUri: string;
  modelLogLoss: number;
  totalGameCount: number;
  seasonWins: number;
  seasonLosses: number;
  seasonOvertimeLosses: number;
  totalModelAccurateGameCount: number;
  draftKingsLogLoss: number;
  draftKingsAccurateGameCount: number;
  draftKingsGameCount: number;
  gameOddsVM: GameOddsVM[];
}

export interface TeamsVM {
  teams: TeamVM[];
  seasonTotals: SeasonTotalsVM;
}

/** A team's season stats without its games. */
export type TeamSummaryVM = Omit<TeamVM, 'gameOddsVM'>;

/** The game fields the log loss chart reads. */
export type ChartGame = Pick<
  GameOddsVM,
  'id' | 'gameDate' | 'hasBeenPlayed' | 'logLoss' | 'winner'
> & {
  bookmakerOdds?: Pick<BookmakerOddsVM, 'bookmakerName' | 'homeOdds' | 'awayOdds'>[];
};

export type SeasonTeamGameVM = ChartGame & {
  homeTeamId: number;
  awayTeamId: number;
};

/** The Teams page's data: each team's stats and every game once (GetSeasonTeams). */
export interface SeasonTeamsVM {
  teams: TeamSummaryVM[];
  seasonTotals: SeasonTotalsVM;
  /** With DraftKings' and Kalshi's odds only */
  games: SeasonTeamGameVM[];
  /** Other books with odds this season, loaded with getSeasonBookmakerOdds when picked */
  otherBookmakers: string[];
}

export interface SeasonBookmakerOddsVM {
  gameId: number;
  homeOdds: number;
  awayOdds: number;
}

/** NHL.com's projected lineup for a game (Game page); null when there isn't one yet */
export interface GameLineupResultVM {
  lineup: GameLineupVM | null;
}

export interface GameLineupVM {
  gameId: number;
  updatedUTC: string;
  /** Versions seen before puck drop; more than one means it changed */
  versionCount: number;
  statusReport: string | null;
  away: TeamLineupVM;
  home: TeamLineupVM;
}

export interface TeamLineupVM {
  forwards: LineupPlayerVM[][];
  defense: LineupPlayerVM[][];
  goalies: LineupPlayerVM[];
  scratched: LineupPlayerVM[];
  injured: LineupPlayerVM[];
  suspended: LineupPlayerVM[];
}

export interface LineupPlayerVM {
  name: string;
  playerId: number | null;
  note: string | null;
}

/** How the market's view of a game moved before puck drop (Game page) */
export interface OddsMovementVM {
  gameId: number;
  startUTC: string;
  series: OddsSeriesVM[];
  move: MarketMoveVM | null;
}

export interface OddsSeriesVM {
  name: string;
  kind: 'model' | 'partner' | 'average' | 'kalshi';
  points: { timeUTC: string; homeProbability: number }[];
}

export interface MarketMoveVM {
  open: number;
  close: number;
  model: number | null;
  /** Close minus open, positive when the market moved toward the model */
  towardModel: number | null;
}

/** How the model's win probabilities held up over a season (Track Record page) */
export interface TrackRecordVM {
  seasonStartYear: number;
  liveOnly: boolean;
  gameCount: number;
  liveGameCount: number;
  modelLogLoss: number | null;
  modelAccuracy: number | null;
  calibration: CalibrationBinVM[];
  logLossByDate: LogLossPointVM[];
  sources: SourceComparisonVM[];
  closingLine: ClosingLineVM | null;
}

export interface CalibrationBinVM {
  low: number;
  high: number;
  games: number;
  meanPredicted: number;
  homeWinRate: number;
}

export interface LogLossPointVM {
  date: string;
  games: number;
  model: number;
  market: number | null;
}

export interface SourceComparisonVM {
  name: string;
  games: number;
  logLoss: number;
  accuracy: number;
  modelLogLoss: number;
  modelAccuracy: number;
}

export interface ClosingLineVM {
  games: number;
  modelCloser: number;
  movedTowardModel: number;
  movedAwayFromModel: number;
  averageMoveTowardModel: number;
}

/** One feature group's effect on the model's prediction (#155) */
export interface PredictionFactorVM {
  factor: string;
  detail: string;
  /** Change in the home team's win probability; positive favors home */
  contribution: number;
}

/** Why the model predicted a game the way it did: baseHomeOdds plus every contribution is its home win probability */
export interface PredictionFactorsVM {
  gameId: number;
  runDateUTC: string;
  baseHomeOdds: number;
  factors: PredictionFactorVM[];
}

/** NHL position letter */
export type PositionCode = 'C' | 'L' | 'R' | 'D' | 'G';

export interface PlayerTeamVM {
  id: number;
  locationName: string;
  teamName: string;
  logoUri: string;
}

/** A box score line: skater fields for skaters, goalie fields for goalies */
export interface PlayerGameStatsVM {
  timeOnIceSeconds: number;
  goals: number;
  assists: number;
  points: number;
  shotsOnGoal: number;
  plusMinus: number;
  shotsAgainst: number;
  saves: number;
  goalsAgainst: number;
  savePct: number | null;
  isStarter: boolean;
}

export interface PlayerGameVM {
  gameId: number;
  gameDate: string;
  gameType: number;
  teamId: number;
  isHome: boolean;
  opponent: PlayerTeamVM;
  teamGoals: number;
  opponentGoals: number;
  /** OTL: lost in overtime or a shootout */
  result: 'W' | 'L' | 'OTL';
  stats: PlayerGameStatsVM;
}

export interface PlayerSeasonTotalsVM {
  gamesPlayed: number;
  timeOnIceSeconds: number;
  goals: number;
  assists: number;
  points: number;
  shotsOnGoal: number;
  plusMinus: number;
  shotsAgainst: number;
  saves: number;
  goalsAgainst: number;
  savePct: number | null;
  goalsAgainstAverage: number | null;
}

/** The Player page (GetPlayer) */
export interface PlayerVM {
  id: number;
  firstName: string;
  lastName: string;
  headShot: string;
  /** From their most recent game; null when they have none */
  position: PositionCode | null;
  sweaterNumber: number | null;
  /** yyyy-MM-dd */
  birthDate: string | null;
  birthCity: string;
  birthStateProvince: string;
  heightInInches: number;
  weightInPounds: number;
  /**
   * The team listing them on the NHL's latest roster report (or, with no recent
   * report, the team of their latest game if it was this season); null when
   * they're on no NHL roster: then lastTeam is who they last played for
   */
  currentTeam: PlayerTeamVM | null;
  /** On currentTeam's injured reserve list */
  isInjuredReserve: boolean;
  /** The team of their most recent game, and its season; null with no games */
  lastTeam: PlayerTeamVM | null;
  lastSeasonStartYear: number | null;
  seasonStartYear: number;
  /** Newest first */
  games: PlayerGameVM[];
  seasonTotals: PlayerSeasonTotalsVM;
}

/**
 * A team's player (GetTeamRoster): this season, those on the NHL's latest
 * roster report; past seasons, those whose latest game that season was for the team
 */
export interface RosterPlayerVM {
  id: number;
  firstName: string;
  lastName: string;
  headShot: string;
  position: PositionCode;
  sweaterNumber: number | null;
  gamesPlayed: number;
  /** On the team's injured reserve list (this season only) */
  isInjuredReserve: boolean;
}

export interface GamePlayerVM {
  id: number;
  firstName: string;
  lastName: string;
  position: PositionCode;
  sweaterNumber: number | null;
  /** null before the game is played */
  stats: PlayerGameStatsVM | null;
}

/** A game's players (GetGamePlayers): once played, who dressed with their lines (hasStats); before, each team's roster */
export interface GamePlayersVM {
  gameId: number;
  hasStats: boolean;
  home: GamePlayerVM[];
  away: GamePlayerVM[];
}

/** A player and when their last game started, for the sitemap */
export interface ActivePlayerVM {
  id: number;
  lastGameDate: string;
}

/** The league leaders in one stat for a regular season (#182; /api/Leaders/GetStatLeaders) */
export interface StatLeadersVM {
  seasonStartYear: number;
  stat: string;
  label: string;
  group: 'skater' | 'goalie';
  /** Goals against average: the lowest leads */
  lowerIsBetter: boolean;
  /** Rate stats only count players with at least this many games; 0 for totals */
  minGamesPlayed: number;
  /** Up to 50, best first */
  leaders: StatLeaderVM[];
}

export interface StatLeaderVM {
  /** Shared on ties (1, 2, 2, 4) */
  rank: number;
  playerId: number;
  firstName: string;
  lastName: string;
  headShot: string;
  position: PositionCode;
  /** The team of their latest game that season */
  teamId: number;
  teamAbbreviation: string;
  teamName: string;
  teamLogoUri: string;
  gamesPlayed: number;
  /** Time on ice per game in seconds; save percentage as a fraction */
  value: number;
}
