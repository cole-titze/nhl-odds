import { useState } from 'react';
import { useIsDarkTheme } from '../hooks/useRootTheme';

interface Props {
  src: string;
  alt: string;
  className?: string;
}

// The API serves NHL `_light` logos (for light backgrounds). Dark themes swap
// in the `_dark` variant, falling back to the light one if a team has none.
const toDarkLogo = (src: string) => src.replace(/_light\.svg(\?|$)/, '_dark.svg$1');

export function TeamLogo({ src, alt, className }: Props) {
  const dark = useIsDarkTheme();
  const [failed, setFailed] = useState<string[]>([]);

  const darkSrc = toDarkLogo(src);
  const current = dark && !failed.includes(darkSrc) ? darkSrc : src;
  if (failed.includes(current)) return null;

  return (
    <img
      src={current}
      alt={alt}
      className={className}
      onError={() => setFailed((f) => [...f, current])}
    />
  );
}
