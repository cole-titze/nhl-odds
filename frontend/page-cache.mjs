// Keeps each rendered page for a few seconds, so a busy page renders about once per TTL per pod
// instead of once per visitor. The HTML is the same for everyone (the theme is applied in the
// browser), apart from the CSP nonce, which is swapped for a fresh one on every hit.
import { randomBytes } from 'node:crypto';

const TTL_MS = 15_000;
// Pages are kept as Buffers, outside the JS heap that NODE_OPTIONS caps
const MAX_BYTES = 64 * 1024 * 1024;
const MAX_PAGE_BYTES = 2 * 1024 * 1024;
const NONCE_RE = /'nonce-([A-Za-z0-9+/=]+)'/;
// Set again on every response, or describe the encoded body rather than the page
const SKIPPED_HEADERS = new Set([
  'content-length',
  'content-encoding',
  'transfer-encoding',
  'etag',
  'vary',
  'date',
  'connection',
  'keep-alive',
  'x-page-cache',
]);

const pages = new Map(); // key -> { expires, headers, body, nonce, nonceOffsets }; oldest first
const rendering = new Map(); // key -> Promise of the page being rendered (null if not cacheable)
let totalBytes = 0;

function remove(key) {
  const page = pages.get(key);
  if (!page) return;
  totalBytes -= page.body.length;
  pages.delete(key);
}

function store(key, page) {
  remove(key);
  pages.set(key, page);
  totalBytes += page.body.length;
  for (const oldest of pages.keys()) {
    if (totalBytes <= MAX_BYTES) break;
    remove(oldest);
  }
}

function lookup(key) {
  const page = pages.get(key);
  if (page && page.expires <= Date.now()) {
    remove(key);
    return undefined;
  }
  return page;
}

function send(res, page) {
  let body = page.body;
  const headers = { ...page.headers };
  if (page.nonce) {
    // A base64 nonce of 16 random bytes is always 24 characters, so the offsets still line up
    const nonce = randomBytes(16).toString('base64');
    body = Buffer.from(body);
    for (const offset of page.nonceOffsets) body.write(nonce, offset, 'latin1');
    headers['content-security-policy'] = headers['content-security-policy'].replace(
      page.nonce,
      nonce,
    );
  }
  res.status(200).set(headers).set('X-Page-Cache', 'hit').end(body);
}

// Copies the response as it streams out; calls done with the page, or null if it can't be cached
function capture(res, done) {
  const chunks = [];
  let size = 0;
  let finished = false;
  const finish = (page) => {
    if (finished) return;
    finished = true;
    done(page);
  };
  const add = (chunk, encoding) => {
    if (chunk == null || size > MAX_PAGE_BYTES) return;
    const buf = Buffer.isBuffer(chunk)
      ? chunk
      : typeof chunk === 'string'
        ? Buffer.from(chunk, typeof encoding === 'string' ? encoding : 'utf8')
        : Buffer.from(chunk.buffer, chunk.byteOffset, chunk.byteLength);
    chunks.push(Buffer.from(buf));
    size += buf.length;
  };

  const write = res.write;
  const end = res.end;
  res.write = function (chunk, encoding, cb) {
    add(chunk, encoding);
    return write.call(this, chunk, encoding, cb);
  };
  res.end = function (chunk, encoding, cb) {
    if (typeof chunk !== 'function') add(chunk, encoding);
    const result = end.call(this, chunk, encoding, cb);
    finish(toPage());
    return result;
  };
  // The visitor left before the page finished
  res.on('close', () => finish(null));

  function toPage() {
    const type = String(res.getHeader('content-type') ?? '');
    if (res.statusCode !== 200 || !type.startsWith('text/html') || res.getHeader('set-cookie'))
      return null;
    if (size > MAX_PAGE_BYTES) return null;

    const headers = {};
    for (const [name, value] of Object.entries(res.getHeaders())) {
      if (!SKIPPED_HEADERS.has(name)) headers[name] = value;
    }
    const body = Buffer.concat(chunks);
    const nonce = String(headers['content-security-policy'] ?? '').match(NONCE_RE)?.[1];
    const nonceOffsets = [];
    if (nonce) {
      for (let i = body.indexOf(nonce); i !== -1; i = body.indexOf(nonce, i + nonce.length)) {
        nonceOffsets.push(i);
      }
    }
    return { expires: Date.now() + TTL_MS, headers, body, nonce, nonceOffsets };
  }
}

export function pageCache() {
  return async (req, res, next) => {
    if (req.method !== 'GET') return next();
    const key = req.originalUrl;

    let page = lookup(key);
    // Requests for a page that's already rendering wait for it instead of rendering it again
    if (!page && rendering.has(key)) page = await rendering.get(key);
    if (page) return send(res, page);
    if (rendering.has(key)) return next();

    let resolve;
    rendering.set(key, new Promise((r) => (resolve = r)));
    capture(res, (rendered) => {
      rendering.delete(key);
      if (rendered) store(key, rendered);
      resolve(rendered);
    });
    res.set('X-Page-Cache', 'miss');
    next();
  };
}
