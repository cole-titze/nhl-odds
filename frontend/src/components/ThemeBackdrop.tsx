import type { ReactNode } from 'react';
import { useActiveThemeId } from '../hooks/useRootTheme';
import { Starfield } from './Starfield';

// Fixed backdrops for the scenic themes. Only the active theme's scene is
// mounted, and it remounts on every switch: some browsers don't restart CSS
// animations on an element that goes display:none and back.
const SCENES: Record<string, ReactNode> = {
  ice: (
    <>
      <IceScratches />
      <div className="rink-line rink-blue rink-top" />
      <div className="rink-line rink-red" />
      <div className="rink-line rink-blue rink-bottom" />
      <div className="rink-circle" />
      <div className="rink-dot" />
    </>
  ),
  stars: (
    <>
      <div className="stars-nebula" />
      <Starfield />
    </>
  ),
  aurora: (
    <>
      <Starfield />
      <div className="aurora-band aurora-top" />
      <div className="aurora-band aurora-1" />
      <div className="aurora-band aurora-2" />
      <div className="aurora-band aurora-fringe" />
      <div className="aurora-band aurora-3" />
    </>
  ),
  team: (
    <>
      <div className="team-glow" />
      <div className="team-logo" />
    </>
  ),
};

export function ThemeBackdrop() {
  const themeId = useActiveThemeId();
  const scene = themeId && SCENES[themeId];
  if (!scene) return null;

  return (
    <div key={themeId} className={`scene scene-${themeId}`} aria-hidden="true">
      {scene}
    </div>
  );
}

// Faint skate marks: long shallow arcs at random positions and angles,
// generated once per page load.
function makeScratches() {
  const out: { d: string; w: number; o: number }[] = [];
  for (let i = 0; i < 90; i++) {
    const x = Math.random() * 1600;
    const y = Math.random() * 1000;
    const len = 120 + Math.random() * 420;
    const angle = Math.random() * Math.PI;
    const dx = Math.cos(angle) * len;
    const dy = Math.sin(angle) * len;
    const bend = (Math.random() - 0.5) * len * 0.35;
    const cx = x + dx / 2 - (dy / len) * bend;
    const cy = y + dy / 2 + (dx / len) * bend;
    out.push({
      d: `M${x.toFixed(0)} ${y.toFixed(0)} Q${cx.toFixed(0)} ${cy.toFixed(0)} ${(x + dx).toFixed(0)} ${(y + dy).toFixed(0)}`,
      w: 0.5 + Math.random() * 1.2,
      o: 0.15 + Math.random() * 0.35,
    });
  }
  return out;
}

const SCRATCHES = makeScratches();

function IceScratches() {
  return (
    <svg className="ice-scratches" viewBox="0 0 1600 1000" preserveAspectRatio="xMidYMid slice">
      {SCRATCHES.map((p, i) => (
        <path key={i} d={p.d} strokeWidth={p.w} strokeOpacity={p.o} />
      ))}
    </svg>
  );
}
