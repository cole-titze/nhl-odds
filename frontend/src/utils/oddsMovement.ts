import type { OddsMovementVM } from '../types';

/** Whether a game has an odds movement chart to show (its section and the Game page's chip) */
export const hasOddsMovement = (data: OddsMovementVM | null) => !!data && data.series.length > 0;
