import { useEffect, useRef } from 'react';

// Drifting star layers, used inside the stars and aurora theme backdrops.
// Each layer is a single tiny element whose box-shadows are the stars, so the
// whole field is a handful of DOM nodes regardless of star count. The shadows
// are random, so they're applied via CSSOM (CSP blocks inline style attributes).
function makeStars(count: number, maxAlpha: number): string {
  const shadows: string[] = [];
  for (let i = 0; i < count; i++) {
    const x = Math.round(Math.random() * 2000);
    const y = Math.round(Math.random() * 2000);
    const a = (0.3 + Math.random() * (maxAlpha - 0.3)).toFixed(2);
    shadows.push(`${x}px ${y}px rgba(255,255,255,${a})`);
  }
  return shadows.join(',');
}

const LAYERS = [
  { className: 'starfield-layer starfield-sm', count: 700, maxAlpha: 0.7 },
  { className: 'starfield-layer starfield-md', count: 180, maxAlpha: 0.9 },
  { className: 'starfield-layer starfield-lg', count: 40, maxAlpha: 1 },
];

export function Starfield() {
  const refs = useRef<(HTMLDivElement | null)[]>([]);

  useEffect(() => {
    LAYERS.forEach((l, i) => {
      const el = refs.current[i];
      if (el) el.style.boxShadow = makeStars(l.count, l.maxAlpha);
    });
  }, []);

  return (
    <div className="starfield">
      {LAYERS.map((l, i) => (
        <div
          key={l.className}
          className={l.className}
          ref={(el) => {
            refs.current[i] = el;
          }}
        />
      ))}
    </div>
  );
}
