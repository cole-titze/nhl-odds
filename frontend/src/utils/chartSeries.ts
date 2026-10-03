// Visual styling for chart series that doesn't rely on color alone (WCAG
// 1.4.1) and keeps every line at 3:1 against the chart background (1.4.11).

// One dash pattern per series slot, so lines differ in shape as well as color
const DASH_PATTERNS = ['', '8 4', '2 3', '10 3 2 3', '4 4', '12 3 2 3 2 3'];

export function dashFor(index: number): string {
  return DASH_PATTERNS[index % DASH_PATTERNS.length];
}

// Card backgrounds a chart can sit on: the plain, Fresh Ice and high contrast
// light themes, and the dark themes (solid page and translucent glass).
const LIGHT_BACKGROUNDS = ['#ffffff', '#fafafa', '#eaf2f7'];
const DARK_BACKGROUNDS = ['#000000', '#141418', '#1c1c20'];
const MIN_CONTRAST = 3;

const toRgb = (hex: string) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16));
const toHex = (rgb: number[]) =>
  `#${rgb.map((c) => Math.round(c).toString(16).padStart(2, '0')).join('')}`;

function luminance(rgb: number[]): number {
  const [r, g, b] = rgb.map((c) => {
    const s = c / 255;
    return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrast(a: number[], b: number[]): number {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi + 0.05) / (lo + 0.05);
}

const cache = new Map<string, string>();

/** The series color, darkened (light themes) or lightened (dark themes) just
 * enough to reach 3:1 against every background it can be drawn on. */
export function readableLineColor(color: string, dark: boolean): string {
  const key = `${color}|${dark}`;
  const hit = cache.get(key);
  if (hit) return hit;

  const backgrounds = (dark ? DARK_BACKGROUNDS : LIGHT_BACKGROUNDS).map(toRgb);
  const target = dark ? [255, 255, 255] : [0, 0, 0];
  const base = toRgb(color);
  let result = color;
  for (let step = 0; step <= 20; step++) {
    const t = step / 20;
    const mixed = base.map((c, i) => c + (target[i] - c) * t);
    if (backgrounds.every((bg) => contrast(mixed, bg) >= MIN_CONTRAST)) {
      result = toHex(mixed);
      break;
    }
  }
  cache.set(key, result);
  return result;
}
