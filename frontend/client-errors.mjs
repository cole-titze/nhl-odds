// Visitors' error reports, on their way from the browser (src/utils/errorReporting.ts) to the
// web API's /api/ClientError:
//
// - Stack traces arrive minified (assets/GamePage-BrA-8FoX.js:1:4821). They're mapped back to
//   the source (src/pages/GamePage.tsx:212:9) with the build's hidden source maps, which
//   scripts/move-sourcemaps.mjs keeps out of the served files.
// - A request for an /assets/ file this build doesn't have is a tab opened before a deploy. It
//   gets a plain 404 (not a rendered page) and is reported as a "chunk" error.
//
// Reports are forwarded with the visitor's IP, so the API's per-visitor limit still applies.
import { readFile } from 'node:fs/promises';
import { SourceMap } from 'node:module';
import { join } from 'node:path';

const MAX_BODY_BYTES = 16 * 1024;
const MAPS_DIR = 'build/sourcemaps/assets';
// Parsed maps are a few MB for the vendor chunk, so only the recently used ones are kept
const MAX_MAPS = 20;
// Chrome: "at fn (https://host/assets/x.js:1:2)"; Firefox and Safari: "fn@https://host/assets/x.js:1:2"
const FRAME = /(?:https?:\/\/[^\s/()]+)?\/assets\/([\w.-]+\.js):(\d+):(\d+)/g;

async function readVersion(buildDir) {
  try {
    return JSON.parse(await readFile(join(buildDir, 'client/version.json'), 'utf8')).version;
  } catch {
    return 'unknown';
  }
}

const maps = new Map(); // file -> Promise<SourceMap | null>

function loadMap(file) {
  let map = maps.get(file);
  if (map) {
    maps.delete(file);
  } else {
    map = readFile(join(MAPS_DIR, `${file}.map`), 'utf8').then(
      (text) => new SourceMap(JSON.parse(text)),
      () => null,
    );
  }
  maps.set(file, map);
  if (maps.size > MAX_MAPS) maps.delete(maps.keys().next().value);
  return map;
}

// "../../src/pages/GamePage.tsx?route-chunk=main" -> "src/pages/GamePage.tsx"
const tidy = (source) =>
  source
    .replace(/\?.*$/, '')
    .replace(/^(\.\.\/|\.\/|\/)+/, '')
    .replace(/^.*?\/(src|node_modules)\//, '$1/');

/** The stack with each frame in this build's code replaced by its source position. */
export async function mapStack(stack) {
  const frames = [...stack.matchAll(FRAME)];
  if (frames.length === 0) return stack;
  const mapped = await Promise.all(
    frames.map(async ([text, file, line, column]) => {
      const map = await loadMap(file);
      // findEntry takes 0-based lines and columns; stack traces are 1-based
      const entry = map?.findEntry(Number(line) - 1, Number(column) - 1);
      if (!entry?.originalSource) return text;
      const name = entry.name ? ` ${entry.name}` : '';
      return `${tidy(entry.originalSource)}:${entry.originalLine + 1}:${entry.originalColumn + 1}${name}`;
    }),
  );
  let i = 0;
  return stack.replace(FRAME, () => mapped[i++]);
}

const tooLarge = () => Object.assign(new Error('too large'), { status: 413 });

function readBody(req) {
  return new Promise((resolve, reject) => {
    if (Number(req.headers['content-length']) > MAX_BODY_BYTES) return reject(tooLarge());
    const chunks = [];
    let size = 0;
    req.on('data', (chunk) => {
      size += chunk.length;
      // Keep reading (and dropping) the rest, so the 413 reaches the browser
      if (size > MAX_BODY_BYTES) return reject(tooLarge());
      chunks.push(chunk);
    });
    req.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
    req.on('error', reject);
  });
}

// The same headers the /api proxy sets (server.mjs setRealIp)
function forwardHeaders(req) {
  const headers = {
    'Content-Type': 'application/json',
    'X-Real-IP': req.socket.remoteAddress ?? '',
  };
  const clientIp = req.headers['cf-connecting-ip'] ?? req.headers['x-real-ip'];
  if (clientIp) headers['CF-Connecting-IP'] = String(clientIp);
  if (req.headers['user-agent']) headers['User-Agent'] = String(req.headers['user-agent']);
  return headers;
}

async function forward(apiUrl, req, report) {
  const res = await fetch(`${apiUrl}/api/ClientError`, {
    method: 'POST',
    headers: forwardHeaders(req),
    body: JSON.stringify(report),
    signal: AbortSignal.timeout(5000),
  });
  return res.status;
}

export function clientErrors({ apiUrl, buildDir = 'build' }) {
  const version = readVersion(buildDir);

  /** POST /api/ClientError: maps the stack, then hands the report to the API. */
  async function report(req, res) {
    // Browsers say where a request came from; reports from other sites' pages are turned away.
    // (Scripts can still post, which is what the API's limits are for.)
    const site = req.headers['sec-fetch-site'];
    if (site && site !== 'same-origin') return res.status(403).end();
    let body;
    try {
      body = JSON.parse(await readBody(req));
    } catch (e) {
      // Close afterwards: the rest of an oversized body isn't read
      if (e.status === 413) res.set('Connection', 'close');
      return res.status(e.status ?? 400).end();
    }
    if (typeof body?.stack === 'string') {
      try {
        body.stack = await mapStack(body.stack);
      } catch {
        // An unmapped stack is still worth saving
      }
    }
    try {
      res.status(await forward(apiUrl, req, body)).end();
    } catch {
      res.status(502).end();
    }
  }

  /** After the static files: an /assets/ file that isn't there. */
  async function missingAsset(req, res) {
    res.status(404).type('text/plain').send('Not found');
    if (!/\.(js|css)$/.test(req.path)) return;
    let page = null;
    try {
      page = req.headers.referer ? new URL(req.headers.referer).pathname : null;
    } catch {
      // Not a URL; leave the page out
    }
    // One message for every missing file, so the API keeps one report per window however many
    // files (or made-up URLs) are asked for; the file goes in the details
    forward(apiUrl, req, {
      kind: 'chunk',
      name: 'MissingAsset',
      message: 'A code file this build does not have was requested (a tab from an older build?)',
      stack: `File: ${req.originalUrl.split('?')[0]}\nCurrent build: ${await version}`,
      page,
      version: `server ${await version}`,
    }).catch(() => {});
  }

  return { report, missingAsset };
}
