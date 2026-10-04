import { useLayoutEffect, useRef } from 'react';
import { useActiveThemeId } from '../hooks/useRootTheme';

// Solid strip at the top edge that iOS Safari 26+ samples to tint the status
// bar (see .status-bar-tint in index.css). Safari doesn't re-sample when the
// strip's color changes through a stylesheet rule, so on every theme switch it
// remounts (a newly shown fixed element gets sampled, like a dialog does) and
// writes its color inline (inline writes repaint live, cascade changes don't).
export function StatusBarTint() {
  const themeId = useActiveThemeId();
  const ref = useRef<HTMLDivElement>(null);

  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) return;
    const color = getComputedStyle(document.documentElement)
      .getPropertyValue('--status-bar')
      .trim();
    el.style.backgroundColor = color || '#000';
  }, [themeId]);

  return <div key={themeId} ref={ref} className="status-bar-tint" aria-hidden="true" />;
}
