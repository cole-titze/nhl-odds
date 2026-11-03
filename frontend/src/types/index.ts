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
