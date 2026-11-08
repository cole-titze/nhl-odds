import { useEffect, useState, type MouseEvent } from 'react';
import type { PageSection } from '../utils/sections';

const NAV_ID = 'section-nav';

// The sticky navbar plus this bar: a section scrolled to lands below both
function stickyOffset(): number {
  const navbar = document.querySelector<HTMLElement>('.app-nav');
  const bar = document.getElementById(NAV_ID);
  return (navbar?.offsetHeight ?? 64) + (bar?.offsetHeight ?? 48) + 12;
}

/**
 * Chips under a page's header that jump to its sections ("On this page"), sticky
 * under the navbar, the chip of the section being read highlighted. Plain
 * `#id` links, so they work before the page's script loads; with script, a
 * click scrolls (smoothly unless reduced motion is asked for) and moves focus to
 * the section, so keyboard and screen reader users continue from there. Shown
 * only with two or more sections.
 */
export function SectionNav({
  sections,
  column = 'max-w-6xl',
}: {
  sections: PageSection[];
  /** The page column's max width, so the chips line up with the content under them */
  column?: string;
}) {
  const [active, setActive] = useState(sections[0]?.id);
  const ids = sections.map((s) => s.id).join(' ');

  // Tell index.css how much the sticky bars cover, so focused content and
  // jumped-to sections clear them (WCAG 2.4.11)
  useEffect(() => {
    const root = document.documentElement;
    const update = () => root.style.setProperty('--sticky-top', `${stickyOffset()}px`);
    update();
    window.addEventListener('resize', update);
    return () => {
      window.removeEventListener('resize', update);
      root.style.removeProperty('--sticky-top');
    };
  }, []);

  // The active chip: the last section whose top has passed under the bars, or
  // the last one once the page can't scroll further
  useEffect(() => {
    const list = ids.split(' ');
    let frame = 0;
    const update = () => {
      frame = 0;
      const line = stickyOffset() + 8;
      const atBottom =
        window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 4;
      let current = list[0];
      for (const id of list) {
        const el = document.getElementById(id);
        if (el && el.getBoundingClientRect().top <= line) current = id;
      }
      setActive(atBottom ? list[list.length - 1] : current);
    };
    const onScroll = () => {
      if (!frame) frame = requestAnimationFrame(update);
    };
    update();
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll);
    return () => {
      window.removeEventListener('scroll', onScroll);
      window.removeEventListener('resize', onScroll);
      cancelAnimationFrame(frame);
    };
  }, [ids]);

  if (sections.length < 2) return null;

  function jump(e: MouseEvent<HTMLAnchorElement>, id: string) {
    const el = document.getElementById(id);
    if (!el) return;
    e.preventDefault();
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const top = el.getBoundingClientRect().top + window.scrollY - stickyOffset();
    window.scrollTo({ top: Math.max(0, top), behavior: reduceMotion ? 'instant' : 'smooth' });
    el.focus({ preventScroll: true });
    history.replaceState(history.state, '', `#${id}`);
    setActive(id);
  }

  return (
    <nav
      id={NAV_ID}
      aria-label="On this page"
      // Spans the full window like the Games filter bar; the chips stay in the page's column and
      // scroll sideways when they don't fit
      className="app-subbar sticky top-14 md:top-16 z-40 mx-[calc(50%-50vw)] py-2 backdrop-blur bg-white/70 dark:bg-surface-950/70 border-b border-surface-200/60 dark:border-white/[0.06]"
    >
      <ul className={`mx-auto ${column} px-5 flex gap-2 overflow-x-auto [scrollbar-width:none]`}>
        {sections.map((s) => (
          <li key={s.id} className="shrink-0">
            <a
              href={`#${s.id}`}
              onClick={(e) => jump(e, s.id)}
              aria-current={active === s.id ? 'location' : undefined}
              className={`inline-flex items-center min-h-8 rounded-full border px-3 py-1 text-xs font-medium whitespace-nowrap transition-colors ${
                active === s.id
                  ? 'border-accent-500 bg-accent-500/10 text-accent-700 dark:text-accent-300'
                  : 'border-surface-300 dark:border-white/15 text-surface-700 dark:text-surface-300 hover:border-accent-500 hover:text-accent-700 dark:hover:text-accent-300'
              }`}
            >
              {s.label}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}
