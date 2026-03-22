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
  modelOdds: number;
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
  logLoss: number;
  modelId: number;
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
