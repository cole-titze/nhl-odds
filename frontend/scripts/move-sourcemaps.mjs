// Moves the client build's source maps (vite.config.ts: sourcemap 'hidden') out of
// build/client, which server.mjs serves, into build/sourcemaps, which it only reads to map
// the stack traces in visitors' error reports (client-errors.mjs). The maps hold the
// original source, so they aren't published.
import { mkdirSync, readdirSync, renameSync } from 'node:fs';
import { join } from 'node:path';

const from = 'build/client/assets';
const to = 'build/sourcemaps/assets';
mkdirSync(to, { recursive: true });
let moved = 0;
for (const file of readdirSync(from)) {
  if (!file.endsWith('.map')) continue;
  renameSync(join(from, file), join(to, file));
  moved++;
}
console.log(`Moved ${moved} source maps to ${to}`);
