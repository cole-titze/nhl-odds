import { useEffect, useState, type RefObject } from 'react';

/**
 * Turns true once the element comes within `margin` of the viewport, and stays true. Without
 * IntersectionObserver (old browsers, tests) it's true right away, so content never stays hidden.
 */
export function useNearViewport(ref: RefObject<Element | null>, margin = '300px'): boolean {
  const [near, setNear] = useState(false);
  const supported = typeof IntersectionObserver !== 'undefined';

  useEffect(() => {
    const element = ref.current;
    if (near || !supported || !element) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) {
          setNear(true);
          observer.disconnect();
        }
      },
      { rootMargin: margin },
    );
    observer.observe(element);
    return () => observer.disconnect();
  }, [ref, margin, near, supported]);

  return near || !supported;
}
