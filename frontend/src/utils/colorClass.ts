// Tailwind only generates classes it can find as literal strings, so every
// series color is spelled out here.

// Series names are drawn in the normal text color (many brand colors fail
// contrast as text); the color itself goes on the border, tint and swatch.
const CHIP_ACTIVE: Record<string, string> = {
  '#3b82f6': 'bg-[#3b82f622] border-[#3b82f6]',
  '#22c55e': 'bg-[#22c55e22] border-[#22c55e]',
  '#f97316': 'bg-[#f9731622] border-[#f97316]',
  '#a855f7': 'bg-[#a855f722] border-[#a855f7]',
  '#53d337': 'bg-[#53d33722] border-[#53d337]',
  '#1493ff': 'bg-[#1493ff22] border-[#1493ff]',
  '#c4a44a': 'bg-[#c4a44a22] border-[#c4a44a]',
  '#e53e3e': 'bg-[#e53e3e22] border-[#e53e3e]',
  '#1e90ff': 'bg-[#1e90ff22] border-[#1e90ff]',
  '#1b3668': 'bg-[#1b366822] border-[#1b3668]',
  '#e44d2e': 'bg-[#e44d2e22] border-[#e44d2e]',
  '#0a4d3c': 'bg-[#0a4d3c22] border-[#0a4d3c]',
  '#14805e': 'bg-[#14805e22] border-[#14805e]',
  '#8b0000': 'bg-[#8b000022] border-[#8b0000]',
  '#7c3aed': 'bg-[#7c3aed22] border-[#7c3aed]',
  '#e879f9': 'bg-[#e879f922] border-[#e879f9]',
  '#fb923c': 'bg-[#fb923c22] border-[#fb923c]',
  '#a78bfa': 'bg-[#a78bfa22] border-[#a78bfa]',
  '#facc15': 'bg-[#facc1522] border-[#facc15]',
  '#34d399': 'bg-[#34d39922] border-[#34d399]',
  '#f87171': 'bg-[#f8717122] border-[#f87171]',
  '#60a5fa': 'bg-[#60a5fa22] border-[#60a5fa]',
  '#c084fc': 'bg-[#c084fc22] border-[#c084fc]',
  '#4ade80': 'bg-[#4ade8022] border-[#4ade80]',
  '#fbbf24': 'bg-[#fbbf2422] border-[#fbbf24]',
};

const SWATCH: Record<string, string> = {
  '#3b82f6': 'bg-[#3b82f6]',
  '#22c55e': 'bg-[#22c55e]',
  '#f97316': 'bg-[#f97316]',
  '#a855f7': 'bg-[#a855f7]',
  '#53d337': 'bg-[#53d337]',
  '#1493ff': 'bg-[#1493ff]',
  '#c4a44a': 'bg-[#c4a44a]',
  '#e53e3e': 'bg-[#e53e3e]',
  '#1e90ff': 'bg-[#1e90ff]',
  '#1b3668': 'bg-[#1b3668]',
  '#e44d2e': 'bg-[#e44d2e]',
  '#0a4d3c': 'bg-[#0a4d3c]',
  '#14805e': 'bg-[#14805e]',
  '#8b0000': 'bg-[#8b0000]',
  '#7c3aed': 'bg-[#7c3aed]',
  '#e879f9': 'bg-[#e879f9]',
  '#fb923c': 'bg-[#fb923c]',
  '#a78bfa': 'bg-[#a78bfa]',
  '#facc15': 'bg-[#facc15]',
  '#34d399': 'bg-[#34d399]',
  '#f87171': 'bg-[#f87171]',
  '#60a5fa': 'bg-[#60a5fa]',
  '#c084fc': 'bg-[#c084fc]',
  '#4ade80': 'bg-[#4ade80]',
  '#fbbf24': 'bg-[#fbbf24]',
  '#737373': 'bg-surface-500',
};

const CHIP_BASE = 'border text-surface-900 dark:text-white';
const CHIP_INACTIVE =
  'border bg-transparent border-surface-400 dark:border-surface-600 text-surface-500 dark:text-surface-400';

export function getChipClasses(color: string, active: boolean): string {
  if (!active) return CHIP_INACTIVE;
  return `${CHIP_BASE} ${CHIP_ACTIVE[color] ?? 'border-surface-500'}`;
}

/** Filled dot in the series color; hollow when the series is hidden, so the state isn't color-only. */
export function getSwatchClasses(color: string, active = true): string {
  const base = 'inline-block w-2 h-2 rounded-full shrink-0';
  if (!active) return `${base} border border-current`;
  return `${base} ${SWATCH[color] ?? 'bg-surface-500'}`;
}
