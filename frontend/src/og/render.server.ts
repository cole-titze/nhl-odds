// Turns a share card (cards.tsx) into a 1200×630 PNG for link previews: Satori lays the
// JSX out as SVG and resvg (WebAssembly, so the image stays free of native modules)
// rasterises it. Server only.
import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import type { ReactElement } from 'react';
import satori, { type SatoriOptions } from 'satori';
import { Resvg, initWasm } from '@resvg/resvg-wasm';

export const OG_WIDTH = 1200;
export const OG_HEIGHT = 630;

const require = createRequire(import.meta.url);

type Font = SatoriOptions['fonts'][number];

// Satori reads woff but not woff2, so these are the static @fontsource files
const FONT_FILES: [pkg: string, file: string, name: string, weight: Font['weight']][] = [
  ['@fontsource/outfit', 'outfit-latin-400-normal.woff', 'Outfit', 400],
  ['@fontsource/outfit', 'outfit-latin-600-normal.woff', 'Outfit', 600],
  ['@fontsource/outfit', 'outfit-latin-800-normal.woff', 'Outfit', 800],
  ['@fontsource/jetbrains-mono', 'jetbrains-mono-latin-500-normal.woff', 'Mono', 500],
  ['@fontsource/jetbrains-mono', 'jetbrains-mono-latin-700-normal.woff', 'Mono', 700],
];

let setup: Promise<Font[]> | null = null;

function loadRenderer(): Promise<Font[]> {
  setup ??= (async () => {
    await initWasm(await readFile(require.resolve('@resvg/resvg-wasm/index_bg.wasm')));
    return Promise.all(
      FONT_FILES.map(async ([pkg, file, name, weight]) => ({
        name,
        weight,
        style: 'normal' as const,
        // The packages only export their CSS, so the files are found next to it
        data: await readFile(join(dirname(require.resolve(pkg)), 'files', file)),
      })),
    );
  })().catch((e) => {
    setup = null;
    throw e;
  });
  return setup;
}

async function renderPng(card: ReactElement): Promise<Buffer> {
  const fonts = await loadRenderer();
  const svg = await satori(card, { width: OG_WIDTH, height: OG_HEIGHT, fonts });
  const png = new Resvg(svg, { fitTo: { mode: 'width', value: OG_WIDTH } }).render().asPng();
  return Buffer.from(png);
}

// Social sites fetch the same image again and again, so finished PNGs are kept, keyed by
// everything drawn on them: a game's image is redrawn only once its odds or score change.
const MAX_IMAGES = 300;
const images = new Map<string, Promise<Buffer>>(); // oldest first

function cachedPng(key: string, draw: () => ReactElement): Promise<Buffer> {
  const hit = images.get(key);
  if (hit) {
    images.delete(key);
    images.set(key, hit);
    return hit;
  }
  const png = renderPng(draw());
  images.set(key, png);
  png.catch(() => images.delete(key));
  for (const oldest of images.keys()) {
    if (images.size <= MAX_IMAGES) break;
    images.delete(oldest);
  }
  return png;
}

/**
 * Answers an image request. `data` is everything the card shows (it keys the cache and the
 * ETag); `maxAge` is how long, in seconds, browsers and crawlers may keep the image.
 */
export async function imageResponse(
  request: Request,
  data: unknown,
  draw: () => ReactElement,
  maxAge: number,
): Promise<Response> {
  const key = JSON.stringify(data);
  const etag = `"${createHash('sha1').update(key).digest('base64url')}"`;
  const headers = {
    'Cache-Control': `public, max-age=${maxAge}`,
    ETag: etag,
  };
  if (request.headers.get('If-None-Match') === etag) {
    return new Response(null, { status: 304, headers });
  }
  const png = await cachedPng(key, draw);
  return new Response(new Uint8Array(png), {
    headers: { ...headers, 'Content-Type': 'image/png', 'Content-Length': String(png.length) },
  });
}

// Team logos are NHL SVGs, drawn into the card as data URLs. Dark cards take the `_dark`
// variant, like the site's dark themes, falling back to the light one.
const LOGO_TTL_MS = 24 * 60 * 60 * 1000;
const LOGO_RETRY_MS = 10 * 60 * 1000;
const logos = new Map<string, { at: number; uri: Promise<string | null> }>();

async function fetchSvg(url: string): Promise<string | null> {
  try {
    const res = await fetch(url, { signal: AbortSignal.timeout(3000) });
    if (!res.ok) return null;
    const svg = await res.text();
    if (!svg.includes('<svg')) return null;
    return `data:image/svg+xml;base64,${Buffer.from(svg).toString('base64')}`;
  } catch {
    return null;
  }
}

/** The logo as a data URL, or null if neither variant could be fetched. */
export function logoDataUri(logoUri: string): Promise<string | null> {
  const now = Date.now();
  const hit = logos.get(logoUri);
  if (hit && now - hit.at < LOGO_TTL_MS) return hit.uri;
  const uri = (async () => {
    const dark = logoUri.replace(/_light\.svg(\?|$)/, '_dark.svg$1');
    return (dark !== logoUri && (await fetchSvg(dark))) || fetchSvg(logoUri);
  })();
  logos.set(logoUri, { at: now, uri });
  // A failed fetch is retried after a few minutes rather than a day
  void uri.then((u) => {
    if (!u) logos.set(logoUri, { at: now - LOGO_TTL_MS + LOGO_RETRY_MS, uri });
  });
  if (logos.size > 200) logos.delete(logos.keys().next().value!);
  return uri;
}
