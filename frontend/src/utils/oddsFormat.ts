import type { OddsFormat } from '../hooks/useOddsFormat';

export function formatOdds(probability: number, format: OddsFormat): string {
  if (format === 'pct') {
    return `${(probability * 100).toFixed(1)}%`;
  }

  if (probability === 0.5) return '+100';

  if (probability > 0.5) {
    return Math.round(-(probability / (1 - probability)) * 100).toString();
  }

  return `+${Math.round(((1 - probability) / probability) * 100)}`;
}

export function formatAmericanOdds(price: number, format: OddsFormat): string {
  if (format === 'vegas') {
    return price > 0 ? `+${price}` : `${price}`;
  }
  // Convert American odds to implied probability
  const prob = price < 0 ? Math.abs(price) / (Math.abs(price) + 100) : 100 / (price + 100);
  return `${(prob * 100).toFixed(0)}%`;
}
