const BUTTON_ACTIVE: Record<string, string> = {
  '#3b82f6': 'text-[#3b82f6] bg-[#3b82f622] border border-[#3b82f666]',
  '#22c55e': 'text-[#22c55e] bg-[#22c55e22] border border-[#22c55e66]',
  '#f97316': 'text-[#f97316] bg-[#f9731622] border border-[#f9731666]',
  '#a855f7': 'text-[#a855f7] bg-[#a855f722] border border-[#a855f766]',
  '#53d337': 'text-[#53d337] bg-[#53d33722] border border-[#53d33766]',
  '#1493ff': 'text-[#1493ff] bg-[#1493ff22] border border-[#1493ff66]',
  '#c4a44a': 'text-[#c4a44a] bg-[#c4a44a22] border border-[#c4a44a66]',
  '#e53e3e': 'text-[#e53e3e] bg-[#e53e3e22] border border-[#e53e3e66]',
  '#1e90ff': 'text-[#1e90ff] bg-[#1e90ff22] border border-[#1e90ff66]',
  '#1b3668': 'text-[#1b3668] bg-[#1b366822] border border-[#1b366866]',
  '#e44d2e': 'text-[#e44d2e] bg-[#e44d2e22] border border-[#e44d2e66]',
  '#0a4d3c': 'text-[#0a4d3c] bg-[#0a4d3c22] border border-[#0a4d3c66]',
  '#14805e': 'text-[#14805e] bg-[#14805e22] border border-[#14805e66]',
  '#8b0000': 'text-[#8b0000] bg-[#8b000022] border border-[#8b000066]',
  '#7c3aed': 'text-[#7c3aed] bg-[#7c3aed22] border border-[#7c3aed66]',
  '#e879f9': 'text-[#e879f9] bg-[#e879f922] border border-[#e879f966]',
  '#fb923c': 'text-[#fb923c] bg-[#fb923c22] border border-[#fb923c66]',
  '#a78bfa': 'text-[#a78bfa] bg-[#a78bfa22] border border-[#a78bfa66]',
  '#facc15': 'text-[#facc15] bg-[#facc1522] border border-[#facc1566]',
  '#34d399': 'text-[#34d399] bg-[#34d39922] border border-[#34d39966]',
  '#f87171': 'text-[#f87171] bg-[#f8717122] border border-[#f8717166]',
  '#60a5fa': 'text-[#60a5fa] bg-[#60a5fa22] border border-[#60a5fa66]',
  '#c084fc': 'text-[#c084fc] bg-[#c084fc22] border border-[#c084fc66]',
  '#4ade80': 'text-[#4ade80] bg-[#4ade8022] border border-[#4ade8066]',
  '#fbbf24': 'text-[#fbbf24] bg-[#fbbf2422] border border-[#fbbf2466]',
};

const BUTTON_INACTIVE = 'text-neutral-500 bg-transparent border border-neutral-600';

const TEXT_CLASS: Record<string, string> = {
  '#3b82f6': 'text-[#3b82f6]',
  '#22c55e': 'text-[#22c55e]',
  '#f97316': 'text-[#f97316]',
  '#a855f7': 'text-[#a855f7]',
  '#53d337': 'text-[#53d337]',
  '#1493ff': 'text-[#1493ff]',
  '#c4a44a': 'text-[#c4a44a]',
  '#e53e3e': 'text-[#e53e3e]',
  '#1e90ff': 'text-[#1e90ff]',
  '#1b3668': 'text-[#1b3668]',
  '#e44d2e': 'text-[#e44d2e]',
  '#0a4d3c': 'text-[#0a4d3c]',
  '#14805e': 'text-[#14805e]',
  '#8b0000': 'text-[#8b0000]',
  '#7c3aed': 'text-[#7c3aed]',
  '#e879f9': 'text-[#e879f9]',
  '#fb923c': 'text-[#fb923c]',
  '#a78bfa': 'text-[#a78bfa]',
  '#facc15': 'text-[#facc15]',
  '#34d399': 'text-[#34d399]',
  '#f87171': 'text-[#f87171]',
  '#60a5fa': 'text-[#60a5fa]',
  '#c084fc': 'text-[#c084fc]',
  '#4ade80': 'text-[#4ade80]',
  '#fbbf24': 'text-[#fbbf24]',
  '#737373': 'text-neutral-500',
};

export function getButtonClasses(color: string, active: boolean): string {
  if (!active) return BUTTON_INACTIVE;
  return BUTTON_ACTIVE[color] ?? BUTTON_INACTIVE;
}

export function getTextClass(color: string): string {
  return TEXT_CLASS[color] ?? 'text-neutral-500';
}
