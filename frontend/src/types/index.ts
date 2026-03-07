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

export interface GameOddsVM {
  id: number;
  gameDate: string;
  homeTeam: MatchupTeamVM | null;
  awayTeam: MatchupTeamVM | null;
  winner: Winner;
  hasBeenPlayed: boolean;
  logLoss: number;
}

export interface SeasonTotalsVM {
  modelLogLoss: number;
  totalGameCount: number;
  totalModelAccurateGameCount: number;
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
  totalModelAccurateGameCount: number;
  gameOddsVM: GameOddsVM[];
}

export interface TeamsVM {
  teams: TeamVM[];
  seasonTotals: SeasonTotalsVM;
}
