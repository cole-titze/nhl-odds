// The phone chat pins the page behind it (body position: fixed), which makes window.scrollY
// read 0 and the page as tall as the screen until it lets go. Anything that measures or moves
// the page's scroll (the Games feed putting the view back after adding older days above it)
// has to wait until then, or it works from the wrong numbers and the page lands on another
// date once the chat closes.

// For a moment after unpinning, iOS can still shift the page as the keyboard animates away,
// and the scroll is put back on each shift; work that moves the scroll waits that out too
const SETTLE_MS = 1000;

let pins = 0;
let settling: ReturnType<typeof setTimeout> | undefined;
const waiting = new Set<() => void>();

/** Whether the page is pinned, or just let go and still settling. */
export function isPagePinned(): boolean {
  return pins > 0 || settling !== undefined;
}

/**
 * Pins the page where it is. The returned function lets it go and scrolls back; it returns
 * that scroll-back, to repeat while the keyboard closes.
 */
export function pinPage(): () => () => void {
  const body = document.body;
  const saved = body.style.cssText;
  const { scrollX, scrollY } = window;
  Object.assign(body.style, { position: 'fixed', top: `${-scrollY}px`, left: '0', right: '0' });
  pins++;
  clearTimeout(settling);
  settling = undefined;
  let released = false;
  return () => {
    const restore = () => window.scrollTo({ left: scrollX, top: scrollY, behavior: 'instant' });
    if (released) return restore;
    released = true;
    body.style.cssText = saved;
    restore();
    if (--pins > 0) return restore;
    settling = setTimeout(() => {
      settling = undefined;
      const run = [...waiting];
      waiting.clear();
      for (const fn of run) fn();
    }, SETTLE_MS);
    return restore;
  };
}

/** Runs `fn` now, or once the page is let go and settled. Returns a cancel function. */
export function whenPageFree(fn: () => void): () => void {
  if (!isPagePinned()) {
    fn();
    return () => {};
  }
  waiting.add(fn);
  return () => waiting.delete(fn);
}
