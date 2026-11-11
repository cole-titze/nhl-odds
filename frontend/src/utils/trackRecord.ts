// Shared by the Track Record page and its lazily loaded charts (components/TrackRecordCharts), kept
// apart from both so the page can use them without pulling in the chart library

// Log loss of always saying 50%: a model has to beat this to be worth anything
export const COIN_FLIP = Math.log(2);
export const MODEL = 'Model';
export const MARKET = "Bookmakers' average";
export const MODEL_COLOR = '#3b82f6';
export const MARKET_COLOR = '#f97316';

export const pct = (v: number, digits = 1) => `${(v * 100).toFixed(digits)}%`;
export const ll = (v: number) => v.toFixed(4);
